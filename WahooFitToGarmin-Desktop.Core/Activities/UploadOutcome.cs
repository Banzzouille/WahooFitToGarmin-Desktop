namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// How an upload attempt ended, which is what decides whether to retry,
    /// whether to delete the source file, and which counter moves.
    /// </summary>
    public enum UploadOutcomeKind
    {
        Success,

        /// <summary>The service already held this activity. Terminal, and not a failure.</summary>
        Duplicate,

        /// <summary>Worth retrying: network trouble, a server error, or a rate limit.</summary>
        TransientFailure,

        /// <summary>Retrying cannot help: a malformed request, a rejected file.</summary>
        PermanentFailure,

        /// <summary>The session was rejected. Renew once, then retry once.</summary>
        Unauthorised,
    }

    public sealed record UploadOutcome(UploadOutcomeKind Kind, string? Message = null, TimeSpan? RetryAfter = null)
    {
        public static UploadOutcome Success(string? message = null) => new(UploadOutcomeKind.Success, message);

        public static UploadOutcome Duplicate(string? message = null) => new(UploadOutcomeKind.Duplicate, message);

        public static UploadOutcome Transient(string? message = null, TimeSpan? retryAfter = null) =>
            new(UploadOutcomeKind.TransientFailure, message, retryAfter);

        public static UploadOutcome Permanent(string? message = null) => new(UploadOutcomeKind.PermanentFailure, message);

        public static UploadOutcome Unauthorised(string? message = null) => new(UploadOutcomeKind.Unauthorised, message);
    }

    /// <summary>
    /// Sends an activity to the service. Separated from the pipeline so the
    /// pipeline's own behaviour can be tested without a network.
    /// </summary>
    public interface IActivityUploader
    {
        Task<UploadOutcome> UploadAsync(
            byte[] content,
            string filePath,
            CancellationToken cancellationToken);
    }
}
