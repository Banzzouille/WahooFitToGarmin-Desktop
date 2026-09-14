using System.Collections.ObjectModel;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.GARMIN;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IToastNotificationsService _toastNotificationsService;
        private readonly ILogger<MainViewModel> _logger;
        private readonly ILogStore _logStore;
        private string? _wahooFolder;
        private string? _garminLogin;
        private string? _garminPwd;
        private bool _keepFile;
        private IClient? _client;

        // The collection lives in the log store now, so that entries logged
        // anywhere — including the core library — reach this view.
        public ObservableCollection<LogEntry> LogEntries => _logStore.Entries;

        public MainViewModel(
            IToastNotificationsService toastNotificationsService,
            ILogger<MainViewModel> logger,
            ILogStore logStore)
        {
            _toastNotificationsService = toastNotificationsService;
            _logger = logger;
            _logStore = logStore;

            _logger.LogInformation("Starting .......");
            DumpSettings();

            if (Directory.Exists(_wahooFolder) && !string.IsNullOrEmpty(_garminLogin) && !string.IsNullOrEmpty(_garminPwd))
            {
                var fw = new FileSystemWatcher
                {
                    Filter = "*.fit",
                    Path = _wahooFolder,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };
                fw.Created += FileIsComing;
            }
            else
            {
                _logger.LogInformation("Please feel correctly yours app settings in settings screen and restart the application to apply them");
            }

            _logger.LogInformation("Starting uploader ......");
        }

        private void FileIsComing(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("A new file is coming => {FileName}", e.Name);
            _toastNotificationsService.ShowSimpleToastNotification("A new file is coming", e.Name);
            UploadAsync(_garminLogin!, _garminPwd!, e.FullPath).ConfigureAwait(false);

            _logger.LogInformation("-------------------------------------------------------------------------------");
        }

        private void DumpSettings()
        {
            _wahooFolder = App.Current.Properties["WahooDropBoxFolder"]?.ToString();
            if (string.IsNullOrEmpty(_wahooFolder))
            {
                _logger.LogInformation("Please select folder to watch for in settings");
            }

            _logger.LogInformation("Wahoo folder to watch for : {WahooFolder}", _wahooFolder);

            _garminLogin = App.Current.Properties["GarminLogin"]?.ToString();
            _garminPwd = App.Current.Properties["GarminPwd"]?.ToString();

            bool.TryParse(App.Current.Properties["KeepUploadedActivityFile"]?.ToString(), out var keepFile);
            _keepFile = keepFile;

            if (string.IsNullOrEmpty(_garminLogin) || string.IsNullOrEmpty(_garminPwd))
            {
                _logger.LogInformation("Please enter your Garmin login and password in settings");
            }
        }

        private async Task UploadAsync(string email, string password, string file)
        {
            if (_client == null || _client.OAuth2Token == null)
            {
                _logger.LogInformation("Connection to Garmin Connect server");
                // Passing the logger through means the core library's failures
                // reach the same pipeline, so they show up in the viewer too.
                _client = await ClientFactory.Create(_logger);
                var authResult = await _client.Authenticate(email, password);
                if (authResult.IsSuccess)
                {
                    _logger.LogInformation("Connection success.");
                }
            }
            else
            {
                _logger.LogInformation("Already logged.");
            }

            try
            {
                _logger.LogInformation("Uploading file {File}", file);
                var response = await _client.UploadActivity(Path.GetExtension(file).Remove(0, 1), File.ReadAllBytes(file), file).ConfigureAwait(false);

                if (response?.DetailedImportResult != null)
                {
                    if (response.DetailedImportResult.uploadUuid != null)
                    {
                        _logger.LogInformation("Activity uploaded {File}", file);
                        _logger.LogInformation(
                            "Activity uploaded :{ServiceMessage}",
                            response.DetailedImportResult.successes?[0].Messages?[0].Content);

                        if (!_keepFile)
                        {
                            File.Delete(file);
                        }
                    }
                    else if (response.DetailedImportResult.failures?.Count > 0)
                    {
                        _logger.LogError(
                            "Failed to upload activity to Garmin : {ServiceMessage}",
                            response.DetailedImportResult.failures[0].Messages?[0].Content);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to upload activity {File}", file);
            }
        }
    }
}
