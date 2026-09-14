using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Watches a folder for activity files. Behind an interface so discovery can
    /// be driven by a test, and so a polling implementation can replace the
    /// event-based one if a platform needs it.
    /// </summary>
    public interface IFolderWatcher : IDisposable
    {
        /// <summary>Raised with the full path of a file that has appeared.</summary>
        event Action<string>? FileAppeared;

        void Watch(string folderPath);

        void Stop();
    }

    /// <summary>
    /// Finds activity files and hands them to the pipeline.
    /// </summary>
    /// <remarks>
    /// Two sources feed the same queue: a watcher for files arriving while the
    /// application runs, and a scan at startup for files that arrived while it
    /// was closed. Both go through the pipeline's idempotence check, so a file
    /// seen by both is processed once.
    /// </remarks>
    public sealed class ActivityDiscovery : IDisposable
    {
        private readonly ISettingsStore _settings;
        private readonly IProcessedActivityRecord _record;
        private readonly IActivityFileStore _files;
        private readonly IFolderWatcher _watcher;
        private readonly ActivityPipeline _pipeline;
        private readonly INotifier _notifier;
        private readonly ILogger _logger;

        private string? _watchedFolder;

        public ActivityDiscovery(
            ISettingsStore settings,
            IProcessedActivityRecord record,
            IActivityFileStore files,
            IFolderWatcher watcher,
            ActivityPipeline pipeline,
            INotifier notifier,
            ILogger logger)
        {
            _settings = settings;
            _record = record;
            _files = files;
            _watcher = watcher;
            _pipeline = pipeline;
            _notifier = notifier;
            _logger = logger;

            _watcher.FileAppeared += OnFileAppeared;
            _settings.Changed += OnSettingsChanged;
        }

        public void Start()
        {
            var folder = _settings.Current.WatchedFolder;

            // The watcher starts before the scan, so a file arriving during the
            // scan is not missed. Anything seen twice is deduplicated by the
            // record rather than processed twice.
            StartWatching(folder);

            if (string.IsNullOrWhiteSpace(folder))
            {
                _logger.LogInformation("Please select folder to watch for in settings");
                return;
            }

            if (!Directory.Exists(folder))
            {
                _logger.LogError(
                    "The configured folder {Folder} does not exist; watching will start when it does",
                    folder);
                return;
            }

            if (_record.IsFirstRun)
            {
                EstablishBaseline(folder);
                return;
            }

            ScanExisting(folder);
        }

        /// <summary>
        /// Records what is already in the folder without uploading any of it.
        /// </summary>
        /// <remarks>
        /// Adding a startup scan to an application that never had one is
        /// dangerous: a user with a year of activity files would see all of them
        /// pushed to the service at once, and cleaning that up on the service's
        /// side is tedious. The first run therefore establishes a baseline. From
        /// the second run onwards the scan does what it is for — catching files
        /// that arrived while the application was closed.
        /// </remarks>
        private void EstablishBaseline(string folder)
        {
            var existing = _files.Enumerate(folder);
            if (existing.Count == 0)
            {
                _record.MarkBaseline([]);
                return;
            }

            var entries = new List<(string, string)>(existing.Count);

            foreach (var path in existing)
            {
                try
                {
                    var content = File.ReadAllBytes(path);
                    entries.Add((ActivityHash.Compute(content), Path.GetFileName(path)));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read {Path} while establishing the baseline", path);
                }
            }

            _record.MarkBaseline(entries);

            _logger.LogInformation(
                "{Count} activity files already in the folder were marked as already processed and will not be uploaded",
                entries.Count);
        }

        private void ScanExisting(string folder)
        {
            var existing = _files.Enumerate(folder);

            foreach (var path in existing)
            {
                _pipeline.Enqueue(path);
            }

            if (existing.Count > 0)
            {
                _logger.LogInformation(
                    "{Count} activity files found in the folder at startup",
                    existing.Count);
            }
        }

        private void StartWatching(string? folder)
        {
            _watcher.Stop();
            _watchedFolder = folder;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            _watcher.Watch(folder);
        }

        private void OnFileAppeared(string path)
        {
            var fileName = Path.GetFileName(path);

            // Announced at detection, as it always was. The startup scan stays
            // quiet: a user restarting the application does not want a
            // notification for every file waiting in the folder.
            _logger.LogInformation("A new file is coming => {FileName}", fileName);
            _notifier.Notify("A new file is coming", fileName);

            _pipeline.Enqueue(path);

            _logger.LogInformation("-------------------------------------------------------------------------------");
        }

        private void OnSettingsChanged(object? sender, UserSettings settings)
        {
            if (string.Equals(settings.WatchedFolder, _watchedFolder, StringComparison.Ordinal))
            {
                return;
            }

            // Changing the watched folder takes effect immediately; the previous
            // behaviour required restarting the application.
            _logger.LogInformation("Wahoo folder to watch for : {WahooFolder}", settings.WatchedFolder);

            StartWatching(settings.WatchedFolder);

            if (!string.IsNullOrWhiteSpace(settings.WatchedFolder) && Directory.Exists(settings.WatchedFolder))
            {
                ScanExisting(settings.WatchedFolder);
            }
        }

        public void Dispose()
        {
            _settings.Changed -= OnSettingsChanged;
            _watcher.FileAppeared -= OnFileAppeared;
            _watcher.Dispose();
        }
    }
}
