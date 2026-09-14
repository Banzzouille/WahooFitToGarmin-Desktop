using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Options;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Contracts.ViewModels;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;
using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    /// <summary>
    /// Presentation for the settings page.
    /// </summary>
    /// <remarks>
    /// Every user setting is read from and written to the settings store, which
    /// is the single source of truth and persists on change. The page no longer
    /// writes to the application's property bag or to its shipped configuration,
    /// and nothing here requires the application to be restarted.
    /// </remarks>
    public class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly AppConfig _appConfig;
        private readonly ISettingsStore _settingsStore;
        private readonly IFolderPicker _folderPicker;
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly ISystemService _systemService;
        private readonly IApplicationInfoService _applicationInfoService;

        private AppTheme _theme;
        private string? _versionDescription;
        private ICommand? _setThemeCommand;
        private ICommand? _githubUrlCommand;
        private ICommand? _selectWahooFolderCommand;

        public SettingsViewModel(
            IOptions<AppConfig> appConfig,
            ISettingsStore settingsStore,
            IFolderPicker folderPicker,
            IThemeSelectorService themeSelectorService,
            ISystemService systemService,
            IApplicationInfoService applicationInfoService)
        {
            _appConfig = appConfig.Value;
            _settingsStore = settingsStore;
            _folderPicker = folderPicker;
            _themeSelectorService = themeSelectorService;
            _systemService = systemService;
            _applicationInfoService = applicationInfoService;
        }

        public AppTheme Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public bool KeepUploadedActivityFile
        {
            get => _settingsStore.Current.KeepUploadedActivityFile;
            set
            {
                if (value == _settingsStore.Current.KeepUploadedActivityFile)
                {
                    return;
                }

                _settingsStore.Update(s => s with { KeepUploadedActivityFile = value });
                OnPropertyChanged();
            }
        }

        public string? GarminLogin
        {
            get => _settingsStore.Current.GarminLogin;
            set
            {
                if (value == _settingsStore.Current.GarminLogin)
                {
                    return;
                }

                _settingsStore.Update(s => s with { GarminLogin = value });
                OnPropertyChanged();
            }
        }

        public string? GarminPwd
        {
            get => _settingsStore.Current.GarminPassword;
            set
            {
                if (value == _settingsStore.Current.GarminPassword)
                {
                    return;
                }

                _settingsStore.Update(s => s with { GarminPassword = value });
                OnPropertyChanged();
            }
        }

        public string? WahooDropBoxFolder
        {
            get => _settingsStore.Current.WatchedFolder;
            private set
            {
                if (value == _settingsStore.Current.WatchedFolder)
                {
                    return;
                }

                _settingsStore.Update(s => s with { WatchedFolder = value });
                OnPropertyChanged();
            }
        }

        public string? VersionDescription
        {
            get => _versionDescription;
            set => SetProperty(ref _versionDescription, value);
        }

        public ICommand SetThemeCommand => _setThemeCommand ??= new RelayCommand<string>(OnSetTheme);

        public ICommand GithubUrlCommand => _githubUrlCommand ??= new RelayCommand(OnGithubUrl);

        public ICommand SelectWahooFolderCommand =>
            _selectWahooFolderCommand ??= new AsyncRelayCommand(OnSelectWahooFolderAsync);

        public void OnNavigatedTo(object parameter)
        {
            VersionDescription = $"{Properties.Resources.AppDisplayName} - {_applicationInfoService.GetVersion()}";
            Theme = _themeSelectorService.GetCurrentTheme();

            // The settings-backed properties read straight through to the store,
            // so they only need a notification to refresh the bindings.
            OnPropertyChanged(nameof(WahooDropBoxFolder));
            OnPropertyChanged(nameof(GarminLogin));
            OnPropertyChanged(nameof(GarminPwd));
            OnPropertyChanged(nameof(KeepUploadedActivityFile));
        }

        public void OnNavigatedFrom()
        {
        }

        private void OnSetTheme(string? themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return;
            }

            var theme = Enum.Parse<AppTheme>(themeName);
            _themeSelectorService.SetTheme(theme);
        }

        private void OnGithubUrl()
        {
            if (!string.IsNullOrWhiteSpace(_appConfig.GithubUrl))
            {
                _systemService.OpenInWebBrowser(_appConfig.GithubUrl);
            }
        }

        private async Task OnSelectWahooFolderAsync()
        {
            var chosen = await _folderPicker.PickFolderAsync(_settingsStore.Current.WatchedFolder);
            if (chosen is null)
            {
                // Cancelled: the configured folder is left untouched.
                return;
            }

            WahooDropBoxFolder = chosen;
        }
    }
}
