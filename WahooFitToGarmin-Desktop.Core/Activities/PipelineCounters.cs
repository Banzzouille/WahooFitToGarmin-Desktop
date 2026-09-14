using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Counts of what the pipeline has done this session.
    /// </summary>
    /// <remarks>
    /// Session-scoped on purpose: they describe what has happened since the
    /// application started, which is what the main page shows. Persisting them
    /// would imply a history feature that does not exist.
    ///
    /// This replaces the <c>Count</c> binding on the main page, which pointed at
    /// a property no view model had.
    /// </remarks>
    public sealed class PipelineCounters : INotifyPropertyChanged
    {
        private int _processed;
        private int _failed;
        private int _duplicates;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Processed => _processed;

        public int Failed => _failed;

        public int Duplicates => _duplicates;

        public void RecordProcessed()
        {
            Interlocked.Increment(ref _processed);
            Notify(nameof(Processed));
        }

        public void RecordFailed()
        {
            Interlocked.Increment(ref _failed);
            Notify(nameof(Failed));
        }

        public void RecordDuplicate()
        {
            Interlocked.Increment(ref _duplicates);
            Notify(nameof(Duplicates));
        }

        private void Notify([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
