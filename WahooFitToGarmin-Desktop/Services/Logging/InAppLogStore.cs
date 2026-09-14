using System.Collections.ObjectModel;
using System.Windows;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.Services.Logging
{
    /// <inheritdoc cref="ILogStore"/>
    public sealed class InAppLogStore : ILogStore
    {
        /// <summary>
        /// Entries retained in memory. A long-running session emits a lot of
        /// lines; without a cap the collection grows until the process ends.
        /// Trimmed entries remain in the log file.
        /// </summary>
        private const int MaxEntries = 500;

        public ObservableCollection<LogEntry> Entries { get; } = new();

        public void Append(DateTime timestamp, LogLevel level, string message)
        {
            var entry = new LogEntry(timestamp, message, level);

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                // No dispatcher yet during early startup, or we are already on
                // the user interface thread.
                AppendCore(entry);
                return;
            }

            dispatcher.BeginInvoke(() => AppendCore(entry));
        }

        private void AppendCore(LogEntry entry)
        {
            Entries.Add(entry);

            while (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }
    }
}
