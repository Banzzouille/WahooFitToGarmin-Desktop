using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Contracts.Services;

namespace WahooFitToGarmin_Desktop.Services.Logging
{
    /// <summary>
    /// Feeds the in-app log viewer from the standard logging pipeline, so the
    /// viewer and the log file show the same stream.
    /// </summary>
    public sealed class InAppLoggerProvider : ILoggerProvider
    {
        private readonly ILogStore _store;

        public InAppLoggerProvider(ILogStore store) => _store = store;

        public ILogger CreateLogger(string categoryName) => new InAppLogger(_store);

        public void Dispose()
        {
        }

        private sealed class InAppLogger : ILogger
        {
            private readonly ILogStore _store;

            public InAppLogger(ILogStore store) => _store = store;

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                // The exception's own text is appended so a failure is readable
                // in the window without opening the log file. The stack trace
                // stays in the file only.
                if (exception is not null)
                {
                    message = $"{message} : {exception.Message}";
                }

                _store.Append(DateTime.Now, logLevel, message);
            }
        }
    }
}
