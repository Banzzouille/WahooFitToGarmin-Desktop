using System;
using System.Windows.Forms;
using System.Windows.Input;

using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Contracts.ViewModels;
using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    public class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly AppConfig _appConfig;
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly ISystemService _systemService;
        private readonly IApplicationInfoService _applicationInfoService;
        private AppTheme _theme;
        private string _versionDescription;
        private string _wahooDropBoxFolder;
        private string _garminLogin;
        private string _garminPwd;
        private ICommand _setThemeCommand;
        private ICommand _githubUrlCommand;
        private ICommand _selectWahooFolderCommand;

        public AppTheme Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public string GarminLogin
        {
            get => _garminLogin;
            set
            {
                SetProperty(ref _garminLogin, value);
                _appConfig.GarminLogin = value;
                App.Current.Properties["GarminLogin"] = value;
            }
        }

        public string GarminPwd
        {
            get => _garminPwd;
            set
            {
                SetProperty(ref _garminPwd, value);
                _appConfig.GarminPwd = value;
                App.Current.Properties["GarminPwd"] = value;
            }
        }

        public string WahooDropBoxFolder
        {
            get => _wahooDropBoxFolder;
            set => SetProperty(ref _wahooDropBoxFolder, value);
        }

        public string VersionDescription
        {
            get => _versionDescription;
            set => SetProperty(ref _versionDescription, value);
        }

        public ICommand SetThemeCommand => _setThemeCommand ??= new RelayCommand<string>(OnSetTheme);

        public ICommand GithubUrlCommand => _githubUrlCommand ??= new RelayCommand(OnGithubUrl);

        public ICommand SelectWahooFolderCommand => _selectWahooFolderCommand ??= new RelayCommand(OnSelectWahooFolder);

        public SettingsViewModel(IOptions<AppConfig> appConfig, IThemeSelectorService themeSelectorService, ISystemService systemService, IApplicationInfoService applicationInfoService)
        {
            _appConfig = appConfig.Value;
            _themeSelectorService = themeSelectorService;
            _systemService = systemService;
            _applicationInfoService = applicationInfoService;
        }

        public void OnNavigatedTo(object parameter)
        {
            VersionDescription = $"{Properties.Resources.AppDisplayName} - {_applicationInfoService.GetVersion()}";
            Theme = _themeSelectorService.GetCurrentTheme();
            WahooDropBoxFolder = App.Current.Properties["WahooDropBoxFolder"].ToString();
            GarminLogin = App.Current.Properties["GarminLogin"].ToString();
            GarminPwd = App.Current.Properties["GarminPwd"].ToString();
        }

        public void OnNavigatedFrom()
        {
        }

        private void OnSetTheme(string themeName)
        {
            var theme = (AppTheme)Enum.Parse(typeof(AppTheme), themeName);
            _themeSelectorService.SetTheme(theme);
        }

        private void OnGithubUrl()
            => _systemService.OpenInWebBrowser(_appConfig.GithubUrl);

        private void OnSelectWahooFolder()
        {
            var folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = false;
            // Show the FolderBrowserDialog.  
            var result = folderDlg.ShowDialog();
            if (result != DialogResult.OK) return;

            WahooDropBoxFolder = folderDlg.SelectedPath;
            _appConfig.WahooDropBoxFolder = folderDlg.SelectedPath;
            App.Current.Properties["WahooDropBoxFolder"] = folderDlg.SelectedPath;
        }
    }
}
