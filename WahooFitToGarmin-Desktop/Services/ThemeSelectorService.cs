using System.Windows;

using ControlzEx.Theming;

using MahApps.Metro.Theming;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Settings;
using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.Services
{
    /// <summary>
    /// Applies and remembers the chosen theme.
    /// </summary>
    /// <remarks>
    /// The choice is stored in the settings store like every other user setting.
    /// It used to live in the application's property bag, which was persisted by
    /// a service that no longer exists — leaving it there would have meant the
    /// theme silently stopped surviving a restart.
    /// </remarks>
    public class ThemeSelectorService : IThemeSelectorService
    {
        private const string HcDarkTheme = "pack://application:,,,/Styles/Themes/HC.Dark.Blue.xaml";
        private const string HcLightTheme = "pack://application:,,,/Styles/Themes/HC.Light.Blue.xaml";

        private readonly ISettingsStore _settingsStore;

        public ThemeSelectorService(ISettingsStore settingsStore) => _settingsStore = settingsStore;

        public void InitializeTheme()
        {
            // TODO WTS: Mahapps.Metro supports syncronization with high contrast but you have to provide custom high contrast themes
            // We've added basic high contrast dictionaries for Dark and Light themes
            // Please complete these themes following the docs on https://mahapps.com/docs/themes/thememanager#creating-custom-themes
            ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri(HcDarkTheme), MahAppsLibraryThemeProvider.DefaultInstance));
            ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri(HcLightTheme), MahAppsLibraryThemeProvider.DefaultInstance));

            SetTheme(GetCurrentTheme());
        }

        public void SetTheme(AppTheme theme)
        {
            if (theme == AppTheme.Default)
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncAll;
                ThemeManager.Current.SyncTheme();
            }
            else
            {
                ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithHighContrast;
                ThemeManager.Current.SyncTheme();
                ThemeManager.Current.ChangeTheme(Application.Current, $"{theme}.Blue", SystemParameters.HighContrast);
            }

            _settingsStore.Update(s => s with { Theme = theme.ToString() });
        }

        public AppTheme GetCurrentTheme() =>
            Enum.TryParse<AppTheme>(_settingsStore.Current.Theme, out var theme)
                ? theme
                : AppTheme.Default;
    }
}
