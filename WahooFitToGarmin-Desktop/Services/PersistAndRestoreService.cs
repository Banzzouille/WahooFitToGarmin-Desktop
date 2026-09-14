using System.Collections;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Helpers;
using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.Services
{
    public class PersistAndRestoreService : IPersistAndRestoreService
    {
        private readonly IFileService _fileService;
        private readonly ILogger<PersistAndRestoreService> _logger;
        private readonly AppConfig _appConfig;
        private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public PersistAndRestoreService(
            IFileService fileService,
            IOptions<AppConfig> appConfig,
            ILogger<PersistAndRestoreService> logger)
        {
            _fileService = fileService;
            _appConfig = appConfig.Value;
            _logger = logger;
        }

        public void PersistData()
        {
            if (App.Current.Properties is null)
            {
                return;
            }

            var (folderPath, fileName) = ResolveTarget();
            if (folderPath is null || fileName is null)
            {
                return;
            }

            try
            {
                _fileService.Save(folderPath, fileName, PropertyBagSerialization.Project(App.Current.Properties));
            }
            catch (Exception ex)
            {
                // A settings file that cannot be written is worth reporting, but
                // it is not a reason to prevent the application from closing.
                _logger.LogError(ex, "Could not write the settings file to {FolderPath}", folderPath);
            }
        }

        public void RestoreData()
        {
            var (folderPath, fileName) = ResolveTarget();
            if (folderPath is null || fileName is null)
            {
                return;
            }

            Dictionary<string, JsonElement>? stored;
            try
            {
                stored = _fileService.Read<Dictionary<string, JsonElement>>(folderPath, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read the settings file from {FolderPath}, starting with defaults", folderPath);
                return;
            }

            if (stored is null)
            {
                return;
            }

            foreach (var (key, value) in stored)
            {
                App.Current.Properties[key] = PropertyBagSerialization.Flatten(value);
            }
        }

        private (string? FolderPath, string? FileName) ResolveTarget()
        {
            var configured = _appConfig.ConfigurationsFolder;
            var fileName = _appConfig.AppPropertiesFileName;

            if (string.IsNullOrWhiteSpace(configured) || string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("Settings storage is not configured; no settings will be persisted or restored");
                return (null, null);
            }

            var relative = configured
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            return (Path.Combine(_localAppData, relative), fileName);
        }

    }
}
