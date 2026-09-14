namespace WahooFitToGarmin.UI.Models;

/// <summary>
/// Configuration that ships with the application and never changes at runtime.
/// </summary>
public class AppConfig
{
    public string? ConfigurationsFolder { get; set; }

    public string? LogsFolder { get; set; }

    public string? SettingsFileName { get; set; }

    public string? GithubUrl { get; set; }
}
