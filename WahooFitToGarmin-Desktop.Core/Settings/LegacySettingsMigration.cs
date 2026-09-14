using System.Text.Json;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Helpers;

namespace WahooFitToGarmin_Desktop.Core.Settings
{
    /// <summary>
    /// Carries settings written by version 1.1.0 into the typed store.
    /// </summary>
    /// <remarks>
    /// The previous format was the user interface's untyped property bag,
    /// serialized and base64-wrapped. Its five keys map onto
    /// <see cref="UserSettings"/> one for one.
    ///
    /// The original file is kept under a backup name rather than overwritten, so
    /// that rolling back to the previous version finds its settings intact.
    /// </remarks>
    public static class LegacySettingsMigration
    {
        public const string LegacyFileName = "AppProperties.json";
        public const string BackupSuffix = ".migrated.bak";

        /// <summary>
        /// Migrates if, and only if, the new file is absent and a legacy file is
        /// present. Returns the migrated settings, or null when there was
        /// nothing to migrate.
        /// </summary>
        public static UserSettings? MigrateIfNeeded(
            IFileService fileService,
            string folderPath,
            string settingsFileName,
            ILogger logger)
        {
            if (File.Exists(Path.Combine(folderPath, settingsFileName)))
            {
                // Already migrated on a previous run.
                return null;
            }

            var legacyPath = Path.Combine(folderPath, LegacyFileName);
            if (!File.Exists(legacyPath))
            {
                // A machine with no previous installation. Not an error.
                return null;
            }

            Dictionary<string, JsonElement>? bag;
            try
            {
                bag = fileService.Read<Dictionary<string, JsonElement>>(folderPath, LegacyFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read the previous settings file; starting with defaults");
                return null;
            }

            if (bag is null || bag.Count == 0)
            {
                return null;
            }

            var migrated = new UserSettings
            {
                WatchedFolder = Value(bag, "WahooDropBoxFolder"),
                GarminLogin = Value(bag, "GarminLogin"),
                GarminPassword = Value(bag, "GarminPwd"),
                KeepUploadedActivityFile =
                    bool.TryParse(Value(bag, "KeepUploadedActivityFile"), out var keep) && keep,
                Theme = Value(bag, "Theme"),
            };

            try
            {
                fileService.Save(folderPath, settingsFileName, migrated);
                File.Move(legacyPath, legacyPath + BackupSuffix, overwrite: true);

                logger.LogInformation(
                    "Settings migrated from the previous format; the original was kept as {BackupName}",
                    LegacyFileName + BackupSuffix);
            }
            catch (Exception ex)
            {
                // The migrated values are still returned: the user keeps their
                // settings for this session even if persisting them failed.
                logger.LogError(ex, "Settings were migrated but could not be persisted");
            }

            return migrated;
        }

        private static string? Value(Dictionary<string, JsonElement> bag, string key) =>
            bag.TryGetValue(key, out var element)
                ? PropertyBagSerialization.Flatten(element)
                : null;
    }
}
