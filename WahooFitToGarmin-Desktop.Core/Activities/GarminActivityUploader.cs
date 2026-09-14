using System.Net;

using Flurl.Http;

using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Uploads through the Garmin client, translating what the service says into
    /// the outcomes the pipeline acts on.
    /// </summary>
    public sealed class GarminActivityUploader : IActivityUploader
    {
        private readonly IGarminSession _session;
        private readonly ILogger _logger;

        public GarminActivityUploader(IGarminSession session, ILogger logger)
        {
            _session = session;
            _logger = logger;
        }

        public async Task<UploadOutcome> UploadAsync(
            byte[] content,
            string filePath,
            CancellationToken cancellationToken)
        {
            var session = await _session.GetAsync(cancellationToken).ConfigureAwait(false);

            if (!session.IsSuccess)
            {
                return session.Failure == SessionFailure.SignInRequired
                    ? UploadOutcome.Permanent(session.Reason)
                    : UploadOutcome.Transient(session.Reason);
            }

            var format = Path.GetExtension(filePath).TrimStart('.');

            _logger.LogInformation("Uploading file {File}", filePath);

            try
            {
                var response = await session.Client!
                    .UploadActivity(format, content, filePath)
                    .ConfigureAwait(false);

                return Interpret(response, filePath);
            }
            catch (FlurlHttpException ex)
            {
                var outcome = FromStatus(ex);

                if (outcome.Kind == UploadOutcomeKind.Unauthorised)
                {
                    // Discard the session so the pipeline's single retry
                    // establishes a fresh one rather than replaying the rejected
                    // credentials.
                    _session.Invalidate();
                }

                return outcome;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Anything unclassified is treated as worth retrying: reporting a
                // permanent failure for a blip would lose the activity.
                _logger.LogError(ex, "Upload of {File} failed", filePath);
                return UploadOutcome.Transient(ex.Message);
            }
        }

        private UploadOutcome Interpret(GARMIN.Dto.Garmin.UploadResponse? response, string filePath)
        {
            var detail = response?.DetailedImportResult;

            if (detail is null)
            {
                return UploadOutcome.Transient("the service returned no result");
            }

            if (detail.uploadUuid is not null)
            {
                var message = detail.successes?.FirstOrDefault()?.Messages?.FirstOrDefault()?.Content;

                _logger.LogInformation("Activity uploaded {File}", filePath);
                _logger.LogInformation("Activity uploaded :{ServiceMessage}", message);

                return UploadOutcome.Success(message);
            }

            var failure = detail.failures?.FirstOrDefault()?.Messages?.FirstOrDefault();

            // The service reports an activity it already holds as a failure with
            // its own code, which is not the same thing as a rejection.
            if (failure?.Code == DuplicateActivityCode)
            {
                return UploadOutcome.Duplicate(failure.Content);
            }

            return UploadOutcome.Permanent(failure?.Content ?? "the service rejected the activity");
        }

        /// <summary>
        /// The code the service uses for an activity it already holds.
        /// </summary>
        private const int DuplicateActivityCode = 202;

        private static UploadOutcome FromStatus(FlurlHttpException ex)
        {
            var status = ex.StatusCode;

            return status switch
            {
                (int)HttpStatusCode.Unauthorized => UploadOutcome.Unauthorised(ex.Message),
                (int)HttpStatusCode.Conflict => UploadOutcome.Duplicate(ex.Message),
                (int)HttpStatusCode.TooManyRequests => UploadOutcome.Transient(ex.Message, RetryAfter(ex)),
                >= 500 => UploadOutcome.Transient(ex.Message),
                >= 400 => UploadOutcome.Permanent(ex.Message),

                // No status at all means the request never got an answer.
                _ => UploadOutcome.Transient(ex.Message),
            };
        }

        private static TimeSpan? RetryAfter(FlurlHttpException ex)
        {
            var header = ex.Call?.Response?.Headers.FirstOrDefault("Retry-After");

            return int.TryParse(header, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : null;
        }
    }
}
