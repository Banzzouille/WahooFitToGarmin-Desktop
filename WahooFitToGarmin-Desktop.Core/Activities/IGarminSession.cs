using WahooFitToGarmin_Desktop.Core.GARMIN;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Why a session could not be obtained.
    /// </summary>
    public enum SessionFailure
    {
        /// <summary>Worth retrying: the network was unavailable, or the service was.</summary>
        Transient,

        /// <summary>No retry can fix this; the user has to act.</summary>
        SignInRequired,
    }

    public sealed record SessionResult
    {
        private SessionResult()
        {
        }

        public IClient? Client { get; private init; }

        public SessionFailure? Failure { get; private init; }

        public string? Reason { get; private init; }

        public bool IsSuccess => Client is not null;

        public static SessionResult Success(IClient client) => new() { Client = client };

        public static SessionResult Failed(SessionFailure failure, string reason) =>
            new() { Failure = failure, Reason = reason };
    }

    /// <summary>
    /// Supplies a usable Garmin session, renewing it when the current one has
    /// expired.
    /// </summary>
    /// <remarks>
    /// The pipeline depends on this rather than on the Garmin client directly,
    /// so that <c>garmin-di-oauth2-core</c> can replace the authentication flow
    /// without the pipeline noticing.
    ///
    /// This is also where the expired-session defect is fixed: validity is asked
    /// of the session before every upload, instead of checking whether a token
    /// object happens to be non-null.
    /// </remarks>
    public interface IGarminSession
    {
        Task<SessionResult> GetAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Discards the current session, so the next request establishes a new
        /// one. Called after the service rejects a request as unauthorised.
        /// </summary>
        void Invalidate();
    }
}
