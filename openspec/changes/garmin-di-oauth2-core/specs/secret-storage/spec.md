## ADDED Requirements

### Requirement: The password is never persisted

The user's Garmin password SHALL be held only for the duration of the sign-in request and then discarded. It SHALL NOT be written to any file, any log, any crash report, or any diagnostic output.

#### Scenario: No password on disk after signing in

- **WHEN** the user signs in successfully
- **THEN** no file written by the application contains the password

#### Scenario: The password is not retained in the application

- **WHEN** sign-in completes, whether it succeeded or failed
- **THEN** the password is no longer held by the application

#### Scenario: A failed sign-in leaves no trace of the password

- **WHEN** sign-in fails and the failure is logged
- **THEN** the log entry contains no password

#### Scenario: No setting stores a password

- **WHEN** the stored settings file is inspected
- **THEN** it contains no password field

### Requirement: Session credentials are encrypted at rest by the operating system

Stored session credentials SHALL be protected using the operating system's own facility — the data protection interface on Windows and the keychain on macOS — so that the encryption key is held by the operating system and bound to the user's session rather than stored alongside the data. The choice of implementation SHALL be made at runtime, not by target framework.

#### Scenario: Stored credentials are not readable as text

- **WHEN** the file or store holding session credentials is examined directly
- **THEN** the credential values are not present in readable form

#### Scenario: Platform selection happens at runtime

- **WHEN** the project files are inspected
- **THEN** no operating-system-specific target framework was introduced to provide this protection

#### Scenario: Credentials survive a restart

- **WHEN** the application is restarted after signing in
- **THEN** the stored credentials are read back successfully and no sign-in is required

### Requirement: An unreadable credential store degrades to signing in again

When stored credentials cannot be read or decrypted, the application SHALL report that signing in is required and continue running. It SHALL NOT crash, and SHALL NOT fail silently.

#### Scenario: A denied or unavailable store is handled

- **WHEN** the operating system refuses access to the protected store
- **THEN** the application starts, logs the condition, and reports that signing in is required

#### Scenario: Corrupt stored credentials are handled

- **WHEN** the stored credential data cannot be decrypted or parsed
- **THEN** the unusable data is discarded and the user is asked to sign in

### Requirement: No secret reaches the log

Tokens, tickets, and passwords SHALL NOT appear in any log output. Response bodies from the token endpoint SHALL NOT be logged verbatim, because the service echoes submitted token values inside some error descriptions. Any token-shaped value SHALL be redacted before a body is written anywhere.

#### Scenario: Token endpoint failures are logged by code, not by body

- **WHEN** a token request fails
- **THEN** the log records the status and the service's error code, and does not contain the response body verbatim

#### Scenario: Echoed tokens are redacted

- **WHEN** a response body containing a token-shaped value is written to any diagnostic output
- **THEN** the token-shaped value is replaced by a redaction marker

#### Scenario: Successful authentication logs no credential

- **WHEN** sign-in, renewal, and upload succeed
- **THEN** the log records the transitions and contains no token, ticket, or password

#### Scenario: A log file can be shared safely

- **WHEN** a user sends their log file to the project
- **THEN** it contains no value that would allow access to their Garmin account

### Requirement: Credentials stored by earlier versions are purged

On first start after this capability is introduced, any Garmin password stored by a previous version SHALL be removed, including copies in files kept as backups by earlier migrations.

#### Scenario: The stored password is removed from settings

- **WHEN** the application starts with settings written by a previous version that contain a password
- **THEN** the password is removed from the settings file

#### Scenario: The migration backup is removed

- **WHEN** a backup file left by an earlier settings migration contains a password
- **THEN** that file is deleted

#### Scenario: Purging happens without user action

- **WHEN** the user upgrades and starts the application
- **THEN** the purge occurs automatically and is recorded in the log without revealing what was purged
