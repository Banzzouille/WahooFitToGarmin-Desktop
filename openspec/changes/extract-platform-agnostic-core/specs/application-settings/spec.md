## ADDED Requirements

### Requirement: Settings are held in a typed store

User settings SHALL be held in a typed object owned by a single store that is the sole source of truth. Settings SHALL NOT be stored in a user-interface framework's global property bag, and no component SHALL keep its own shadow copy.

#### Scenario: One source of truth

- **WHEN** a setting is changed through the settings page
- **THEN** the store holds the new value, and no other copy of that value exists

#### Scenario: Settings are readable without a user interface

- **WHEN** the core library reads settings in a test host with no user interface
- **THEN** the values are available

#### Scenario: Unknown values do not crash the application

- **WHEN** the stored settings file contains a value that cannot be parsed
- **THEN** the application starts, logs the problem, and falls back to the default for that setting

### Requirement: Application configuration is separate from user settings

Values that ship with the application and never change at runtime SHALL be kept separate from values the user edits. User settings SHALL NOT be written back into the application's configuration file.

#### Scenario: Configuration is read-only at runtime

- **WHEN** the application runs
- **THEN** the shipped configuration file is never written to

#### Scenario: User settings are written to their own file

- **WHEN** a user setting changes
- **THEN** it is persisted to the user settings file in the user's data directory

### Requirement: Settings changes take effect without a restart

A change to a user setting SHALL take effect immediately. The application SHALL NOT require a restart for any setting, and SHALL NOT instruct the user to restart.

#### Scenario: Changing the watched folder applies immediately

- **WHEN** the user selects a different folder
- **THEN** watching stops on the previous folder and starts on the new one, without restarting the application

#### Scenario: Changing retention applies to the next upload

- **WHEN** the user toggles the keep-uploaded-file option
- **THEN** the next completed upload honours the new value

#### Scenario: No restart instruction is emitted

- **WHEN** settings are incomplete or have just been changed
- **THEN** no message instructing the user to restart the application is produced

### Requirement: Settings are persisted when they change

A changed setting SHALL be persisted promptly, so that it survives an abrupt termination rather than depending on a clean shutdown.

#### Scenario: A setting survives an abrupt termination

- **WHEN** a setting is changed and the process is terminated without a clean shutdown
- **THEN** the new value is present on the next start

#### Scenario: Persistence failure is reported

- **WHEN** settings cannot be written, for example because the directory is read-only
- **THEN** the failure is logged and the application continues running with the in-memory value

### Requirement: Settings written by earlier versions are migrated

On first start after this capability is introduced, settings stored in the previous format SHALL be read and migrated into the new store. The previous file SHALL be preserved as a backup rather than overwritten in place.

#### Scenario: Previous settings are carried over

- **WHEN** the application starts with a settings file written by the previous version
- **THEN** the watched folder, credentials, retention option, and theme are present in the new store

#### Scenario: The previous file is preserved

- **WHEN** migration completes
- **THEN** the previous file remains on disk under a backup name

#### Scenario: Migration runs once

- **WHEN** the application is started again after a successful migration
- **THEN** the new store is used directly and migration is not repeated

#### Scenario: A missing previous file is not an error

- **WHEN** the application starts on a machine with no previous settings
- **THEN** it starts with defaults and does not report a migration failure

### Requirement: Incomplete configuration is reported clearly

When required settings are missing, the application SHALL state which setting is missing and SHALL continue running so the user can supply it.

#### Scenario: Missing watched folder is reported

- **WHEN** no watched folder is configured
- **THEN** the log states that a folder must be selected, and the application keeps running

#### Scenario: Watching starts as soon as configuration becomes valid

- **WHEN** the user supplies the missing folder
- **THEN** watching begins immediately without a restart

#### Scenario: A configured folder that no longer exists is reported

- **WHEN** the configured folder has been deleted or is unavailable
- **THEN** the situation is logged and the application keeps running rather than failing at startup
