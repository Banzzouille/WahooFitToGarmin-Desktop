using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Helpers
{
    /// <summary>
    /// One line in the in-app log viewer.
    /// </summary>
    /// <remarks>
    /// This type used to append to a file from its own constructor, on the user
    /// interface thread, to a path relative to the working directory. Persisting
    /// is now the logging pipeline's job; this is a plain value.
    ///
    /// <see cref="LogDateTime"/> and <see cref="LogMessage"/> keep their names
    /// because <c>MainPage.xaml</c> binds to them.
    /// </remarks>
    public sealed class LogEntry
    {
        public LogEntry(string message, LogLevel level = LogLevel.Information)
            : this(DateTime.Now, message, level)
        {
        }

        public LogEntry(DateTime timestamp, string message, LogLevel level)
        {
            LogDateTime = timestamp;
            LogMessage = message;
            Level = level;
        }

        public DateTime LogDateTime { get; }

        public string LogMessage { get; }

        /// <summary>
        /// Carried for completeness and for future use; the current view binds
        /// only the timestamp and the message.
        /// </summary>
        public LogLevel Level { get; }
    }
}
