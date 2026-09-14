using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    public sealed record PipelineOptions
    {
        /// <summary>
        /// Attempts for a transient failure, the first included. Three is enough
        /// to ride out a dropped connection without turning a real outage into a
        /// long silence.
        /// </summary>
        public int MaxAttempts { get; init; } = 3;

        public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>Multiplier applied to the delay after each failed attempt.</summary>
        public double BackoffFactor { get; init; } = 4;
    }

    /// <summary>
    /// Discovers, prepares and uploads activity files.
    /// </summary>
    /// <remarks>
    /// Discovery and processing are separated by a queue consumed by a single
    /// worker. The discovery side does nothing but enqueue, so it never blocks
    /// and never performs input or output.
    ///
    /// Processing is serial by choice: it keeps log output in a comprehensible
    /// order, avoids several simultaneous authentication attempts against a
    /// service known to rate-limit, and removes a class of concurrency bug from
    /// code that had no tests at all until now.
    /// </remarks>
    public sealed class ActivityPipeline
    {
        private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleReader = true });

        private readonly ISettingsStore _settings;
        private readonly IProcessedActivityRecord _record;
        private readonly IActivityUploader _uploader;
        private readonly IReadOnlyList<IActivityTransformation> _transformations;
        private readonly IFileSystemProbe _probe;
        private readonly FileReadinessWaiter _readiness;
        private readonly IActivityFileStore _files;
        private readonly ILogger _logger;
        private readonly PipelineOptions _options;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;

        public ActivityPipeline(
            ISettingsStore settings,
            IProcessedActivityRecord record,
            IActivityUploader uploader,
            IEnumerable<IActivityTransformation> transformations,
            IFileSystemProbe probe,
            FileReadinessWaiter readiness,
            IActivityFileStore files,
            ILogger logger,
            PipelineOptions? options = null,
            Func<TimeSpan, CancellationToken, Task>? delay = null)
        {
            _settings = settings;
            _record = record;
            _uploader = uploader;
            _transformations = transformations.ToList();
            _probe = probe;
            _readiness = readiness;
            _files = files;
            _logger = logger;
            _options = options ?? new PipelineOptions();
            _delay = delay ?? Task.Delay;
        }

        public PipelineCounters Counters { get; } = new();

        /// <summary>
        /// Queues a file. Performs no input or output, so the caller — a file
        /// system watcher callback, typically — returns immediately.
        /// </summary>
        public void Enqueue(string path) => _queue.Writer.TryWrite(path);

        /// <summary>
        /// Consumes the queue until cancelled. One activity at a time.
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await foreach (var path in _queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await ProcessAsync(path, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // One activity failing must never stop the worker, or a
                    // single bad file would silently end all processing.
                    _logger.LogError(ex, "Unexpected failure while processing {Path}", path);
                    Counters.RecordFailed();
                }
            }
        }

        private async Task ProcessAsync(string path, CancellationToken cancellationToken)
        {
            var readiness = await _readiness.WaitAsync(path, _delay, cancellationToken).ConfigureAwait(false);

            switch (readiness)
            {
                case ReadinessResult.Vanished:
                    _logger.LogInformation("{Path} disappeared before it could be read", path);
                    return;

                case ReadinessResult.TimedOut:
                    Counters.RecordFailed();
                    return;
            }

            byte[] content;
            try
            {
                content = await _files.ReadAsync(path, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read {Path}", path);
                Counters.RecordFailed();
                return;
            }

            var hash = ActivityHash.Compute(content);
            var fileName = Path.GetFileName(path);

            if (_record.Contains(hash))
            {
                _logger.LogInformation("{FileName} has already been processed; skipping", fileName);
                return;
            }

            byte[] payload;
            try
            {
                payload = Transform(content, fileName);
            }
            catch (Exception ex)
            {
                // Uploading the untransformed file instead would silently give
                // the user something other than what they asked for.
                _logger.LogError(ex, "Could not prepare {FileName} for upload; it will not be uploaded", fileName);
                Counters.RecordFailed();
                return;
            }

            var outcome = await UploadWithRetryAsync(payload, path, cancellationToken).ConfigureAwait(false);

            await ApplyOutcomeAsync(outcome, path, fileName, hash).ConfigureAwait(false);
        }

        private byte[] Transform(byte[] content, string fileName)
        {
            var payload = content;

            foreach (var transformation in _transformations)
            {
                payload = transformation.Apply(payload, fileName);
            }

            return payload;
        }

        private async Task<UploadOutcome> UploadWithRetryAsync(
            byte[] payload,
            string path,
            CancellationToken cancellationToken)
        {
            var delay = _options.InitialBackoff;
            var renewed = false;

            for (var attempt = 1; ; attempt++)
            {
                var outcome = await _uploader.UploadAsync(payload, path, cancellationToken).ConfigureAwait(false);

                switch (outcome.Kind)
                {
                    case UploadOutcomeKind.Success:
                    case UploadOutcomeKind.Duplicate:
                    case UploadOutcomeKind.PermanentFailure:
                        // Terminal, whichever way it went. Retrying a permanent
                        // failure wastes time and floods the log.
                        return outcome;

                    case UploadOutcomeKind.Unauthorised:
                        if (renewed)
                        {
                            return UploadOutcome.Permanent(outcome.Message ?? "the service rejected the session twice");
                        }

                        // One renewal, one more attempt. The session abstraction
                        // renews on the next request after being invalidated.
                        renewed = true;
                        continue;

                    case UploadOutcomeKind.TransientFailure:
                        if (attempt >= _options.MaxAttempts)
                        {
                            return outcome;
                        }

                        // A stated retry delay wins over our own schedule.
                        var wait = outcome.RetryAfter ?? delay;
                        _logger.LogInformation(
                            "Upload of {Path} failed for a transient reason; retrying in {Delay}",
                            path,
                            wait);

                        await _delay(wait, cancellationToken).ConfigureAwait(false);
                        delay = TimeSpan.FromTicks((long)(delay.Ticks * _options.BackoffFactor));
                        continue;
                }
            }
        }

        private async Task ApplyOutcomeAsync(UploadOutcome outcome, string path, string fileName, string hash)
        {
            switch (outcome.Kind)
            {
                case UploadOutcomeKind.Success:
                    _record.Mark(hash, fileName, ActivityOutcome.Uploaded);
                    Counters.RecordProcessed();

                    if (!_settings.Current.KeepUploadedActivityFile)
                    {
                        await DeleteAsync(path).ConfigureAwait(false);
                    }

                    break;

                case UploadOutcomeKind.Duplicate:
                    // The service already holds it, so deleting would probably be
                    // safe — but "probably" is not good enough for irreversibly
                    // removing a user's file. It stays, and the user can remove
                    // it themselves.
                    _record.Mark(hash, fileName, ActivityOutcome.Duplicate);
                    Counters.RecordDuplicate();
                    _logger.LogInformation(
                        "{FileName} is already present on the service; the local file is kept",
                        fileName);
                    break;

                default:
                    // Not recorded as processed: a failure must be retryable on a
                    // later run. The file stays so the activity is not lost.
                    Counters.RecordFailed();
                    _logger.LogError(
                        "Failed to upload {FileName} : {Reason}",
                        fileName,
                        outcome.Message ?? "no reason reported");
                    break;
            }
        }

        private async Task DeleteAsync(string path)
        {
            try
            {
                await _files.DeleteAsync(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The upload succeeded, so this is not an upload failure. Saying
                // so and moving on is better than reporting a false failure.
                _logger.LogWarning(ex, "Uploaded {Path} but could not delete it", path);
            }
        }
    }
}
