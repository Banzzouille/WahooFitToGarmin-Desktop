// UseWindowsForms swaps the implicit using set for the Windows Forms one, which
// drops System.IO. Imported explicitly rather than reinstating it globally.
using System.IO;

using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Events;

using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.Services.Logging
{
    /// <summary>
    /// Builds the Serilog logger that writes the rolling log file.
    /// </summary>
    public static class FileLoggingSetup
    {
        // Fixed by the application-logging specification: roll daily, roll again
        // at 10 MB, keep at most 7 files. Not user-configurable, because nobody
        // will tune them and the point is that disk usage stays bounded.
        private const long FileSizeLimitBytes = 10L * 1024 * 1024;
        private const int RetainedFileCountLimit = 7;

        private const string OutputTemplate =
            "{Timestamp:u} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Resolves the folder the log files live in: a <c>Logs</c> folder under
        /// the user's local application data, beside the configuration folder.
        /// </summary>
        public static string ResolveLogDirectory(IConfiguration configuration)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var configured = configuration
                .GetSection(nameof(AppConfig))
                .GetValue<string>(nameof(AppConfig.LogsFolder));

            // Fall back to a sane default rather than writing to the working
            // directory, which is what the previous implementation did.
            var relative = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine("WahooFitToGarmin_Desktop", "Logs")
                : configured.Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(localAppData, relative);
        }

        /// <summary>
        /// Configures the rolling file sink. A sink that cannot be opened is
        /// reported through Serilog's own diagnostic channel and then dropped:
        /// the application keeps running without a log file rather than failing
        /// to start.
        /// </summary>
        public static void Configure(LoggerConfiguration configured, IConfiguration configuration)
        {
            configured
                .MinimumLevel.Information()
                .Enrich.FromLogContext();

            try
            {
                var directory = ResolveLogDirectory(configuration);
                Directory.CreateDirectory(directory);

                configured.WriteTo.File(
                    path: Path.Combine(directory, "wahoo-fit-to-garmin-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: RetainedFileCountLimit,
                    outputTemplate: OutputTemplate,
                    shared: false);
            }
            catch (Exception ex)
            {
                // Serilog swallows sink failures at write time, but configuring
                // a sink against an unwritable directory throws here. Losing the
                // file is acceptable; failing to start is not.
                Serilog.Debugging.SelfLog.WriteLine(
                    "File logging disabled, the log directory could not be prepared: {0}", ex);
            }
        }
    }
}
