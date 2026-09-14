using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Abstracts the file system so readiness can be tested without one.
    /// </summary>
    public interface IFileSystemProbe
    {
        bool Exists(string path);

        /// <summary>Length and last write time, or null when unavailable.</summary>
        (long Length, DateTime LastWriteUtc)? Stat(string path);

        /// <summary>True when the file can be opened for reading.</summary>
        bool CanOpenForRead(string path);
    }

    /// <inheritdoc cref="IFileSystemProbe"/>
    public sealed class FileSystemProbe : IFileSystemProbe
    {
        public bool Exists(string path) => File.Exists(path);

        public (long Length, DateTime LastWriteUtc)? Stat(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? (info.Length, info.LastWriteTimeUtc) : null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public bool CanOpenForRead(string path)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public enum ReadinessResult
    {
        Ready,

        /// <summary>The file stopped existing while we waited.</summary>
        Vanished,

        /// <summary>Still changing when the timeout expired.</summary>
        TimedOut,
    }

    /// <summary>
    /// Decides when an activity file has finished being written.
    /// </summary>
    /// <remarks>
    /// The watcher reports a file when it appears, not when writing finishes.
    /// A sync client keeps writing afterwards, which is how a partial file ended
    /// up being uploaded.
    ///
    /// Readiness is judged on content stability: length and last write time
    /// unchanged across consecutive checks, separated by a quiet interval. The
    /// obvious alternative — open the file exclusively and treat success as
    /// "the writer is done" — is unreliable, because file locking is advisory on
    /// Unix and an exclusive open can succeed while another process is still
    /// writing. A successful read-only open is used as confirmation, never as
    /// the primary signal. See design.md, D2.
    /// </remarks>
    public sealed class FileReadinessWaiter
    {
        private readonly IFileSystemProbe _probe;
        private readonly ILogger _logger;

        public FileReadinessWaiter(
            IFileSystemProbe probe,
            ILogger logger,
            TimeSpan? quietInterval = null,
            int requiredStableChecks = 2,
            TimeSpan? timeout = null)
        {
            _probe = probe;
            _logger = logger;
            QuietInterval = quietInterval ?? TimeSpan.FromSeconds(1);
            RequiredStableChecks = requiredStableChecks;
            Timeout = timeout ?? TimeSpan.FromMinutes(2);
        }

        public TimeSpan QuietInterval { get; }

        /// <summary>
        /// How many consecutive unchanged observations are required. More than
        /// one, because a slow network pause during a download can make a file
        /// look settled for a moment.
        /// </summary>
        public int RequiredStableChecks { get; }

        public TimeSpan Timeout { get; }

        public async Task<ReadinessResult> WaitAsync(
            string path,
            Func<TimeSpan, CancellationToken, Task> delay,
            CancellationToken cancellationToken)
        {
            var deadline = DateTimeOffset.UtcNow + Timeout;
            (long Length, DateTime LastWriteUtc)? previous = null;
            var stableChecks = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_probe.Exists(path))
                {
                    return ReadinessResult.Vanished;
                }

                var current = _probe.Stat(path);

                if (current is not null && previous is not null && current == previous)
                {
                    stableChecks++;

                    if (stableChecks >= RequiredStableChecks && _probe.CanOpenForRead(path))
                    {
                        return ReadinessResult.Ready;
                    }
                }
                else
                {
                    stableChecks = 0;
                }

                previous = current;

                if (DateTimeOffset.UtcNow >= deadline)
                {
                    _logger.LogError(
                        "{Path} was still changing after {Timeout}; it will not be uploaded",
                        path,
                        Timeout);

                    return ReadinessResult.TimedOut;
                }

                await delay(QuietInterval, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
