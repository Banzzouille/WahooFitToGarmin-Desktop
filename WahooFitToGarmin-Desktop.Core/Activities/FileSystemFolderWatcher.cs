using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Folder watching through <see cref="FileSystemWatcher"/>.
    /// </summary>
    /// <remarks>
    /// Top level only and filtered to activity files, matching what the
    /// application has always done.
    ///
    /// The callback only raises the event, which the discovery layer turns into
    /// an enqueue. Nothing is read here: this runs on a thread pool callback and
    /// blocking it would be the start of a different set of problems.
    ///
    /// Event delivery differs on macOS, which maps to a different kernel
    /// mechanism. <c>avalonia-ui-port</c> carries the task that verifies this
    /// against a real sync folder there; the interface exists so a polling
    /// implementation can be substituted without touching anything else.
    /// </remarks>
    public sealed class FileSystemFolderWatcher : IFolderWatcher
    {
        private readonly ILogger _logger;
        private FileSystemWatcher? _watcher;

        public FileSystemFolderWatcher(ILogger logger) => _logger = logger;

        public event Action<string>? FileAppeared;

        public void Watch(string folderPath)
        {
            Stop();

            try
            {
                _watcher = new FileSystemWatcher
                {
                    Path = folderPath,
                    Filter = ActivityFileStore.ActivityFilePattern,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                };

                _watcher.Created += OnCreated;
                _watcher.Renamed += OnRenamed;
                _watcher.Error += OnError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not watch {Folder}", folderPath);
            }
        }

        public void Stop()
        {
            if (_watcher is null)
            {
                return;
            }

            _watcher.Created -= OnCreated;
            _watcher.Renamed -= OnRenamed;
            _watcher.Error -= OnError;
            _watcher.Dispose();
            _watcher = null;
        }

        private void OnCreated(object sender, FileSystemEventArgs e) => FileAppeared?.Invoke(e.FullPath);

        // Sync clients often write to a temporary name and rename into place, in
        // which case the file never raises Created under its final name.
        private void OnRenamed(object sender, RenamedEventArgs e) => FileAppeared?.Invoke(e.FullPath);

        private void OnError(object sender, ErrorEventArgs e) =>
            _logger.LogError(e.GetException(), "The folder watcher reported an error");

        public void Dispose() => Stop();
    }
}
