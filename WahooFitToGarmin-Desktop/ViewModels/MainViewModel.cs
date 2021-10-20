using System;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<LogEntry> LogEntries { get; set; }
        public MainViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            LogEntries.Add(new LogEntry("Starting ......."));

        }
    }
}
