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
    /// serialized and base64-wrapped. Four of its five keys map onto
    /// <see cref="UserSettings"/> one for one.
    ///
    /// The fifth, the stored password, is deliberately dropped. Version 1.1
    /// kept it on disk; this version does not, because the sign-in flow accepts
    /// a password and exchanges it for a token without ever persisting it.
    /// Carrying the old one forward would reintroduce, at the very first launch,
    /// exactly the thing that design removes. The user signs in once instead.
    ///
    /// The original file is deleted once its values have been taken. It holds a
    /// password in clear text — version 1.1 stored one, and the settings file is
    /// base64 encoded rather than encrypted, which obscures nothing from anyone
    /// who looks. Leaving it behind under a backup name would keep that password
    /// on disk indefinitely, in a file the user does not know exists, while they
    /// upgraded to a version that deliberately stops storing it. Rolling back is
    /// worth less than not leaving a password lying around.
    /// </remarks>
    public static class LegacySettingsMigration
    {
        public const string LegacyFileName = "AppProperties.json";

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
                KeepUploadedActivityFile =
                    bool.TryParse(Value(bag, "KeepUploadedActivityFile"), out var keep) && keep,
                Theme = Value(bag, "Theme"),
            };

            try
            {
                fileService.Save(folderPath, settingsFileName, migrated);
                File.Delete(legacyPath);

                // Saying the password was dropped matters: the user is about to
                // be asked to sign in again and would otherwise read that as a
                // fault rather than as the intended behaviour.
                logger.LogInformation(
                    "Settings migrated from the previous format. {LegacyName} has been deleted, "
                    + "because it held your password in clear text. You will be asked to sign in "
                    + "again: this version does not keep a password on disk",
                    LegacyFileName);
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
