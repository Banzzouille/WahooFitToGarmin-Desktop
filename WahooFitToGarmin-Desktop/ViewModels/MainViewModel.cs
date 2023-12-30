using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.GARMIN;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IToastNotificationsService _toastNotificationsService;
        private string _wahooFolder;
        private string _garminLogin;
        private string _garminPwd;
        private bool _keepFile;
        private IClient _client;

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public MainViewModel(IToastNotificationsService toastNotificationsService)
        {

            _toastNotificationsService = toastNotificationsService;
            LogEntries = new ObservableCollection<LogEntry> { new LogEntry("Starting .......") };
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
                Log("Please feel correctly yours app settings in settings screen and restart the application to apply them");
            }
            Log("Starting uploader ......");

        }

        private void FileIsComing(object sender, FileSystemEventArgs e)
        {
            Log($"A new file is coming => {e.Name}");
            _toastNotificationsService.ShowSimpleToastNotification("A new file is coming", e.Name);
            UploadAsync(_garminLogin, _garminPwd, e.FullPath).ConfigureAwait(false);

            Log("-------------------------------------------------------------------------------");
        }

        private void DumpSettings()
        {
            _wahooFolder = App.Current.Properties["WahooDropBoxFolder"]?.ToString();
            if (string.IsNullOrEmpty(_wahooFolder))
                Log("Please select folder to watch for in settings");

            Log($"Wahoo folder to watch for : {_wahooFolder}");

            _garminLogin = App.Current.Properties["GarminLogin"]?.ToString();
            _garminPwd = App.Current.Properties["GarminPwd"]?.ToString();

            bool.TryParse(App.Current.Properties["KeepUploadedActivityFile"]?.ToString(), out var keepFile);
            _keepFile = keepFile;

            if (string.IsNullOrEmpty(_garminLogin) || string.IsNullOrEmpty(_garminPwd))
                Log("Please enter your Garmin login and password in settings");
        }

        private void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(
                () => { LogEntries.Add(new LogEntry(message)); });
        }

        private async Task UploadAsync(string email, string password, string file)
        {
            Debug.WriteLine($"{nameof(MainViewModel)}.{nameof(UploadAsync)}");
            
            if (_client == null || _client.OAuth2Token == null)
            {
                Log("Connection to Garmin Connect server");
                _client = await ClientFactory.Create();
                var authResult = await _client.Authenticate(email, password);
                if (authResult.IsSuccess)
                {
                    Log("Connection success.");
                }
            }
            else
            {
                Log("Already logged.");
            }

            try
            {
                Log($"Uploading file {file}");
                var response = await _client.UploadActivity(Path.GetExtension(file).Remove(0, 1), File.ReadAllBytes(file), file).ConfigureAwait(false);

                if (response != null)
                {
                    if (response.DetailedImportResult != null)
                    {
                        if (response.DetailedImportResult.uploadUuid != null)
                        {
                            Log($"Activity uploaded {file}");
                            Log($"Activity uploaded :{response.DetailedImportResult.successes[0].Messages?[0].Content}");
                            if (!_keepFile)
                                System.IO.File.Delete(file);
                        }
                        else if (response.DetailedImportResult.failures.Count > 0)
                        {
                            Log($"Failed to upload activity to Garmin : {response.DetailedImportResult.failures[0].Messages?[0].Content}");
                        }
                    }
                }


            }
            catch (Exception e)
            {
                Log($"Failed to upload activity {file} : {e.Message}");
            }
        }
    }
}
