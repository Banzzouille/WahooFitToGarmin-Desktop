using System.Collections.ObjectModel;
using System.ComponentModel;

using WahooFitToGarmin.UI.Services.Logging;

using WahooFitToGarmin_Desktop.Core.Activities;

namespace WahooFitToGarmin.UI.ViewModels;

/// <summary>
/// The main page: the activity log and the counters.
/// </summary>
/// <remarks>
/// Display only. The watcher, the settings reading, the authentication, the
/// upload and the deletion that used to live in a view model of this name are
/// all in the core library now.
/// </remarks>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly PipelineCounters _counters;

    public MainViewModel(InAppLogStore logStore, ActivityPipeline pipeline)
    {
        LogEntries = logStore.Entries;
        _counters = pipeline.Counters;

        _counters.PropertyChanged += OnCountersChanged;
    }

    public ObservableCollection<LogEntry> LogEntries { get; }

    public int Processed => _counters.Processed;

    public int Failed => _counters.Failed;

    public int Duplicates => _counters.Duplicates;

    private void OnCountersChanged(object? sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(e.PropertyName);

    public void Dispose() => _counters.PropertyChanged -= OnCountersChanged;
}
