using System.Reflection;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Options;

using WahooFitToGarmin.UI.Models;
using WahooFitToGarmin.UI.Services;

using WahooFitToGarmin_Desktop.Core.DeviceEmulation;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.UI.ViewModels;

/// <summary>
/// The settings page.
/// </summary>
/// <remarks>
/// Every value reads and writes straight through the settings store, which
/// persists on change. Nothing here needs the application restarted.
/// </remarks>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStore _settings;
    private readonly IFolderPicker _folderPicker;
    private readonly ThemeService _theme;
    private readonly AppConfig _appConfig;

    public SettingsViewModel(
        ISettingsStore settings,
        IFolderPicker folderPicker,
        ThemeService theme,
        IOptions<AppConfig> appConfig)
    {
        _settings = settings;
        _folderPicker = folderPicker;
        _theme = theme;
        _appConfig = appConfig.Value;

        SelectFolderCommand = new AsyncRelayCommand(SelectFolderAsync);
        OpenProjectPageCommand = new RelayCommand(OpenProjectPage);
    }

    public IReadOnlyList<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>();

    public AppTheme SelectedTheme
    {
        get => _theme.Current;
        set
        {
            if (value == _theme.Current)
            {
                return;
            }

            _theme.Set(value);
            OnPropertyChanged();
        }
    }

    public string? WatchedFolder => _settings.Current.WatchedFolder;

    public bool KeepUploadedActivityFile
    {
        get => _settings.Current.KeepUploadedActivityFile;
        set
        {
            if (value == _settings.Current.KeepUploadedActivityFile)
            {
                return;
            }

            _settings.Update(s => s with { KeepUploadedActivityFile = value });
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<EmulatedDevice> Devices { get; } = DeviceCatalogue.All;

    public bool EmulateDevice
    {
        get => _settings.Current.EmulateDevice;
        set
        {
            if (value == _settings.Current.EmulateDevice)
            {
                return;
            }

            _settings.Update(s => s with { EmulateDevice = value });
            OnPropertyChanged();
        }
    }

    public EmulatedDevice? SelectedDevice
    {
        get => DeviceCatalogue.Find(_settings.Current.EmulatedDeviceId);
        set
        {
            if (value?.Id == _settings.Current.EmulatedDeviceId)
            {
                return;
            }

            _settings.Update(s => s with { EmulatedDeviceId = value?.Id });
            OnPropertyChanged();
        }
    }

    public string? GarminLogin
    {
        get => _settings.Current.GarminLogin;
        set
        {
            if (value == _settings.Current.GarminLogin)
            {
                return;
            }

            _settings.Update(s => s with { GarminLogin = value });
            OnPropertyChanged();
        }
    }

    public string? GarminPassword
    {
        get => _settings.Current.GarminPassword;
        set
        {
            if (value == _settings.Current.GarminPassword)
            {
                return;
            }

            _settings.Update(s => s with { GarminPassword = value });
            OnPropertyChanged();
        }
    }

    /// <remarks>
    /// Read from assembly metadata rather than from the assembly's file path,
    /// which is empty under single-file publishing.
    /// </remarks>
    public string Version =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    public AsyncRelayCommand SelectFolderCommand { get; }

    public RelayCommand OpenProjectPageCommand { get; }

    private async Task SelectFolderAsync()
    {
        var chosen = await _folderPicker.PickFolderAsync(_settings.Current.WatchedFolder);
        if (chosen is null)
        {
            // Cancelled: the configured folder is untouched.
            return;
        }

        _settings.Update(s => s with { WatchedFolder = chosen });
        OnPropertyChanged(nameof(WatchedFolder));
    }

    private void OpenProjectPage()
    {
        if (string.IsNullOrWhiteSpace(_appConfig.GithubUrl))
        {
            return;
        }

        // Works on both platforms: the shell resolves the default browser.
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _appConfig.GithubUrl,
            UseShellExecute = true,
        });
    }
}
