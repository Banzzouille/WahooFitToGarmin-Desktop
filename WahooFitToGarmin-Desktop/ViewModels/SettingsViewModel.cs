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
        private ICommand _setThemeCommand;
        private ICommand _githubUrlCommand;
        private ICommand _selectWahooFolderCommand;

        public AppTheme Theme
        {
            get { return _theme; }
            set { SetProperty(ref _theme, value); }
        }

        public string WahooDropBoxFolder
        {
            get { return _wahooDropBoxFolder; }
            set { SetProperty(ref _wahooDropBoxFolder, value); }
        }

        public string VersionDescription
        {
            get { return _versionDescription; }
            set { SetProperty(ref _versionDescription, value); }
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
