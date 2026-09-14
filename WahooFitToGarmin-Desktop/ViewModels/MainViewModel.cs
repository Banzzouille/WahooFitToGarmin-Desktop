using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.ViewModels
{
    /// <summary>
    /// Presentation for the main page: the activity log and the counters.
    /// </summary>
    /// <remarks>
    /// This view model used to be the application. It built the file system
    /// watcher, read settings out of a global property bag, authenticated,
    /// uploaded and deleted files — none of which could be tested, and none of
    /// which a non-WPF user interface could have reused.
    ///
    /// All of that lives in the core library now. What is left is display.
    /// </remarks>
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ILogStore _logStore;
        private readonly PipelineCounters _counters;

        public MainViewModel(ILogStore logStore, ActivityPipeline pipeline)
        {
            _logStore = logStore;
            _counters = pipeline.Counters;

            _counters.PropertyChanged += OnCountersChanged;
        }

        public ObservableCollection<LogEntry> LogEntries => _logStore.Entries;

        public int Processed => _counters.Processed;

        public int Failed => _counters.Failed;

        public int Duplicates => _counters.Duplicates;

        private void OnCountersChanged(object? sender, PropertyChangedEventArgs e)
        {
            // The counter names match the properties exposed here.
            OnPropertyChanged(e.PropertyName);
        }

        public void Dispose() => _counters.PropertyChanged -= OnCountersChanged;
    }
}
