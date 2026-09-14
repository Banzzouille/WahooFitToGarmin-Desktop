using Avalonia;
using Avalonia.Styling;

using WahooFitToGarmin.UI.Models;

using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.UI.Services;

/// <summary>
/// Applies and remembers the chosen theme.
/// </summary>
/// <remarks>
/// The same three choices the application always offered, mapped onto Avalonia's
/// theme variants. Default follows the operating system, which is what the
/// previous implementation's synchronisation mode did.
///
/// The bundled high-contrast dictionaries are gone. They were template stubs the
/// developer was expected to complete — the previous code said so in a comment —
/// and reimplementing incomplete accessibility theming on a new framework would
/// be inventing a feature rather than porting one. The operating system's own
/// accessibility settings apply instead.
/// </remarks>
public sealed class ThemeService
{
    private readonly ISettingsStore _settings;

    public ThemeService(ISettingsStore settings) => _settings = settings;

    public AppTheme Current =>
        Enum.TryParse<AppTheme>(_settings.Current.Theme, out var theme) ? theme : AppTheme.Default;

    public void Initialise() => Apply(Current);

    public void Set(AppTheme theme)
    {
        Apply(theme);
        _settings.Update(s => s with { Theme = theme.ToString() });
    }

    private static void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
