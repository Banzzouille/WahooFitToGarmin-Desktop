using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Helpers;

namespace WahooFitToGarmin_Desktop.Contracts.Services
{
    /// <summary>
    /// The bounded collection of log entries shown in the application window.
    /// </summary>
    /// <remarks>
    /// This is one of two sinks fed by the single logging pipeline, the other
    /// being the rolling file. Anything logged through <c>ILogger</c> reaches
    /// both, including messages emitted by the core library, which previously
    /// appeared nowhere.
    /// </remarks>
    public interface ILogStore
    {
        ObservableCollection<LogEntry> Entries { get; }

        /// <summary>
        /// Appends an entry, marshalling onto the user interface thread and
        /// trimming the oldest entries once the cap is reached.
        /// </summary>
        void Append(DateTime timestamp, LogLevel level, string message);
    }
}
