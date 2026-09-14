## ADDED Requirements

### Requirement: Autostart is controlled from within the application

The application SHALL offer a setting that registers it to start with the user's session, and that removes the registration when switched off. The user SHALL NOT be asked to create or delete system entries by hand.

#### Scenario: Enabling registers the application

- **WHEN** the user enables autostart
- **THEN** the application is registered to start with the user's session on that operating system

#### Scenario: Disabling removes the registration

- **WHEN** the user disables autostart
- **THEN** the registration is removed and the application no longer starts with the session

#### Scenario: The setting reflects reality

- **WHEN** the settings page is opened
- **THEN** the toggle shows whether the application is actually registered, not merely what was last requested

#### Scenario: No manual procedure is documented

- **WHEN** the documentation describes starting automatically
- **THEN** it refers to the setting rather than to placing shortcuts or writing system files by hand

### Requirement: Autostart works on both supported operating systems

Registration SHALL be implemented for Windows and for macOS behind a single abstraction, so that the rest of the application is unaware of the mechanism.

#### Scenario: Windows registration

- **WHEN** autostart is enabled on Windows and the user signs in again
- **THEN** the application starts

#### Scenario: macOS registration

- **WHEN** autostart is enabled on macOS and the user logs in again
- **THEN** the application starts

#### Scenario: The mechanism is hidden behind one abstraction

- **WHEN** the source is inspected
- **THEN** the platform-specific registration lives behind a single abstraction rather than being branched at call sites

### Requirement: Registration is per user, not machine-wide

Autostart SHALL be registered for the current user only, and SHALL NOT require administrative privileges.

#### Scenario: No elevation is requested

- **WHEN** the user enables autostart
- **THEN** the application does not request administrative rights

#### Scenario: Other users are unaffected

- **WHEN** autostart is enabled by one user on a shared machine
- **THEN** other users of that machine are not affected

### Requirement: Registration is idempotent and tolerates staleness

Enabling autostart when it is already enabled SHALL not create a second registration. A registration pointing at a path where the application no longer exists SHALL NOT cause a repeated silent failure at every login.

#### Scenario: Enabling twice creates one registration

- **WHEN** autostart is enabled while already enabled
- **THEN** exactly one registration exists

#### Scenario: A stale registration is recognised

- **WHEN** the application starts and finds a registration pointing at a path that no longer exists
- **THEN** it repairs or removes that registration rather than leaving it

#### Scenario: Moving the application is handled

- **WHEN** the application has been moved since autostart was enabled and the user opens the settings page
- **THEN** the toggle reflects the true state, and re-enabling points the registration at the current location

#### Scenario: Removal leaves nothing behind

- **WHEN** autostart is disabled
- **THEN** no registration entry or file created by the application remains

### Requirement: Previous manual autostart instructions are superseded

The documentation SHALL state that a manually created startup shortcut from an earlier version no longer works, and SHALL direct the user to the setting instead.

#### Scenario: The user is told the old shortcut is dead

- **WHEN** a user upgrading from an earlier version reads the documentation
- **THEN** it states that the previous manual shortcut no longer works because the executable's name and layout changed

#### Scenario: The replacement is stated

- **WHEN** that note is read
- **THEN** it points to the autostart setting as the replacement
