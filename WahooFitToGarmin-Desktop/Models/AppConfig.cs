namespace WahooFitToGarmin_Desktop.Models
{
    /// <summary>
    /// Configuration that ships with the application and never changes at
    /// runtime.
    /// </summary>
    /// <remarks>
    /// This type used to carry user settings as well — the watched folder, the
    /// Garmin credentials, the keep-uploaded-file option — which were also held
    /// in the user interface's property bag. Two sources of truth for the same
    /// values, kept in step by hand. Those now live in
    /// <c>UserSettings</c>, owned by the settings store.
    /// </remarks>
    public class AppConfig
    {
        public string? ConfigurationsFolder { get; set; }

        public string? LogsFolder { get; set; }

        public string? SettingsFileName { get; set; }

        public string? GithubUrl { get; set; }
    }
}
