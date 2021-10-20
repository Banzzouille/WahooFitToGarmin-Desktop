using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Notifications;
using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IToastNotificationsService _toastNotificationsService;
        private string wahooFolder;
        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public MainViewModel(IToastNotificationsService toastNotificationsService)
        {
            _toastNotificationsService = toastNotificationsService;
            LogEntries = new ObservableCollection<LogEntry> { new LogEntry("Starting .......") };
            DumpSettings();


            var fw = new FileSystemWatcher
            {
                Filter = "*.fit",
                Path = wahooFolder,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            fw.Created += fileIsComing;
        }

        private void fileIsComing(object sender, FileSystemEventArgs e)
        {
            Log($"A new file is coming => {e.Name}");
            _toastNotificationsService.ShowSimpleToastNotification("A new file is coming", e.Name);
        }

        private void DumpSettings()
        {
            wahooFolder = App.Current.Properties["WahooDropBoxFolder"].ToString();
            if (string.IsNullOrEmpty(wahooFolder))
                Log("Please select folder to watch for in settings");

            Log($"Wahoo folder to watch for : {wahooFolder}");
        }

        private void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(
                () => { LogEntries.Add(new LogEntry(message)); });
        }
    }
}
