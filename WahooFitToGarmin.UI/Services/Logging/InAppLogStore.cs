using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Platform;

namespace WahooFitToGarmin.UI.Services.Logging;

/// <summary>
/// The bounded collection of log entries shown in the window.
/// </summary>
/// <remarks>
/// One of two sinks fed by a single logging pipeline, the other being the
/// rolling file. Anything logged reaches both, including messages from the core
/// library, which previously appeared nowhere.
/// </remarks>
public sealed class InAppLogStore
{
    /// <summary>
    /// Entries kept in memory. A long session emits a lot of lines; trimmed
    /// entries remain in the log file.
    /// </summary>
    private const int MaxEntries = 500;

    private readonly IUiDispatcher _dispatcher;

    public InAppLogStore(IUiDispatcher dispatcher) => _dispatcher = dispatcher;

    public ObservableCollection<LogEntry> Entries { get; } = [];

    public void Append(DateTime timestamp, LogLevel level, string message) =>
        _dispatcher.Post(() =>
        {
            Entries.Add(new LogEntry(timestamp, message, level));

            while (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        });
}
