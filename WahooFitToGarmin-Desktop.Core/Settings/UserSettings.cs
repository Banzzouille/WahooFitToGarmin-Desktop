namespace WahooFitToGarmin_Desktop.Core.Settings
{
    /// <summary>
    /// The settings a user edits, as a single typed value.
    /// </summary>
    /// <remarks>
    /// This replaces the untyped property bag the user interface used to hold
    /// settings in, which was also a second source of truth beside the
    /// application's configuration object.
    ///
    /// The Garmin credentials are here because that is where they live today.
    /// <c>garmin-di-oauth2-core</c> removes them entirely; nothing new should be
    /// built on them.
    /// </remarks>
    public sealed record UserSettings
    {
        public string? WatchedFolder { get; init; }

        public string? GarminLogin { get; init; }

        public string? GarminPassword { get; init; }

        public bool KeepUploadedActivityFile { get; init; }

        public string? Theme { get; init; }

        /// <summary>
        /// True when the pipeline has everything it needs to run.
        /// </summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(WatchedFolder)
            && !string.IsNullOrWhiteSpace(GarminLogin)
            && !string.IsNullOrWhiteSpace(GarminPassword);
    }
}
