using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin.UI.Services.Logging;

/// <summary>One line in the in-app log viewer.</summary>
public sealed class LogEntry
{
    public LogEntry(DateTime timestamp, string message, LogLevel level)
    {
        LogDateTime = timestamp;
        LogMessage = message;
        Level = level;
    }

    public DateTime LogDateTime { get; }

    public string LogMessage { get; }

    public LogLevel Level { get; }

    /// <summary>True for entries the user should notice.</summary>
    public bool IsProblem => Level >= LogLevel.Warning;
}
