## MODIFIED Requirements

### Requirement: Settings written by earlier versions are migrated

On first start after this capability is introduced, settings stored in the previous format SHALL be read and migrated into the new store. The previous file SHALL be preserved as a backup rather than overwritten in place, **unless it contains credentials**, in which case it SHALL be deleted once its non-credential values have been carried over. Credentials SHALL NOT be carried into the new store.

#### Scenario: Previous settings are carried over

- **WHEN** the application starts with a settings file written by the previous version
- **THEN** the watched folder, retention option, and theme are present in the new store

#### Scenario: Credentials are not carried over

- **WHEN** the previous settings file contains a Garmin password
- **THEN** the password is not written into the new store

#### Scenario: A credential-bearing previous file is deleted, not backed up

- **WHEN** migration completes from a file that contained a password
- **THEN** that file is removed rather than kept under a backup name

#### Scenario: A credential-free previous file is preserved

- **WHEN** migration completes from a file that contained no credentials
- **THEN** the previous file remains on disk under a backup name

#### Scenario: Migration runs once

- **WHEN** the application is started again after a successful migration
- **THEN** the new store is used directly and migration is not repeated

#### Scenario: A missing previous file is not an error

- **WHEN** the application starts on a machine with no previous settings
- **THEN** it starts with defaults and does not report a migration failure

### Requirement: Application configuration is separate from user settings

Values that ship with the application and never change at runtime SHALL be kept separate from values the user edits. User settings SHALL NOT be written back into the application's configuration file. The user settings file SHALL be stored as plain, readable JSON, since it no longer holds any secret.

#### Scenario: Configuration is read-only at runtime

- **WHEN** the application runs
- **THEN** the shipped configuration file is never written to

#### Scenario: User settings are written to their own file

- **WHEN** a user setting changes
- **THEN** it is persisted to the user settings file in the user's data directory

#### Scenario: The settings file is readable

- **WHEN** the user opens the settings file in a text editor
- **THEN** its content is JSON they can read, with no obfuscation wrapper

#### Scenario: The settings file holds no secret

- **WHEN** the settings file is inspected
- **THEN** it contains no password and no session credential, those being held only in the protected credential store
