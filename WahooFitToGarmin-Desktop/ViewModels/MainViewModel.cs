using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WahooFitToGarmin_Desktop.Contracts.Services;
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
        private ApiClient _api;

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

        private async Task UploadAsync(string email,string password,string file)
        {
            Debug.WriteLine($"{nameof(MainViewModel)}.{nameof(UploadAsync)}");

            Log("Connection to Garmin Connect server");
                _api = new ApiClient(email, password);
                var resultInitAuth = await _api.InitAuth();
                Log(resultInitAuth);
            
            try
            {
                Log($"Uploading file {file}");
                var response = await _api.UploadActivity(file).ConfigureAwait(false);

                var result = response.DetailedImportResult;

                if (result.Failures.Any())
                {
                    foreach (var failure in result.Failures)
                    {
                        if (failure.Messages.Any())
                        {
                            foreach (var message in failure.Messages)
                            {
                                if (message.Code == 202)
                                {
                                    Log($"Activity already uploaded {result.FileName}" );
                                }
                                else
                                {
                                    Log($"Failed to upload activity to Garmin. Message: {message}" );
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(response.DetailedImportResult.UploadId))
                {
                    Log($"Successful upload for file {file}");
                    if(!_keepFile)
                        System.IO.File.Delete(file);
                }

            }
            catch (Exception e)
            {
                Log($"Failed to upload workout {file} : {e.Message}");
            }
        }
    }
}
