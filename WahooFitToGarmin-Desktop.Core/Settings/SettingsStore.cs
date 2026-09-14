using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;

namespace WahooFitToGarmin_Desktop.Core.Settings
{
    /// <summary>
    /// File-backed settings store.
    /// </summary>
    /// <remarks>
    /// Loads on construction, persists on every change, and raises
    /// <see cref="Changed"/> afterwards. Failures are reported and swallowed:
    /// unreadable settings fall back to defaults, and an unwritable file leaves
    /// the in-memory value in place. Neither is a reason to stop the
    /// application.
    /// </remarks>
    public sealed class SettingsStore : ISettingsStore
    {
        private readonly IFileService _fileService;
        private readonly ILogger<SettingsStore> _logger;
        private readonly string _folderPath;
        private readonly string _fileName;
        private readonly object _gate = new();

        private UserSettings _current = new();

        public SettingsStore(
            IFileService fileService,
            ILogger<SettingsStore> logger,
            string folderPath,
            string fileName)
        {
            _fileService = fileService;
            _logger = logger;
            _folderPath = folderPath;
            _fileName = fileName;

            Load();
        }

        public UserSettings Current
        {
            get
            {
                lock (_gate)
                {
                    return _current;
                }
            }
        }

        public event EventHandler<UserSettings>? Changed;

        public void Update(Func<UserSettings, UserSettings> change)
        {
            UserSettings updated;

            lock (_gate)
            {
                updated = change(_current);
                if (updated == _current)
                {
                    return;
                }

                _current = updated;
            }

            Persist(updated);
            Changed?.Invoke(this, updated);
        }

        private void Load()
        {
            try
            {
                // Runs at most once: it does nothing when the new file already
                // exists, which it does from the first save onwards.
                var migrated = LegacySettingsMigration.MigrateIfNeeded(
                    _fileService, _folderPath, _fileName, _logger);

                if (migrated is not null)
                {
                    _current = migrated;
                    return;
                }

                var stored = _fileService.Read<UserSettings>(_folderPath, _fileName);
                if (stored is not null)
                {
                    _current = stored;
                }
            }
            catch (Exception ex)
            {
                // A settings file we cannot parse must not stop the application;
                // starting with defaults and saying so is the useful behaviour.
                _logger.LogError(
                    ex,
                    "Could not read the settings file from {FolderPath}, starting with defaults",
                    _folderPath);
            }
        }

        private void Persist(UserSettings settings)
        {
            try
            {
                _fileService.Save(_folderPath, _fileName, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not write the settings file to {FolderPath}; the change is kept in memory only",
                    _folderPath);
            }
        }
    }
}
