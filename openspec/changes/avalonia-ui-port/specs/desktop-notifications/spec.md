## ADDED Requirements

### Requirement: Notification on activity detection

The application SHALL notify the user when a new activity file is detected in the watched folder, using the operating system's native notification mechanism on Windows and on macOS. The notification SHALL identify the file concerned.

#### Scenario: Notification is raised on Windows

- **WHEN** a `.fit` file appears in the watched folder while running on Windows
- **THEN** a native Windows notification is raised naming the detected file

#### Scenario: Notification is raised on macOS

- **WHEN** a `.fit` file appears in the watched folder while running on macOS
- **THEN** a native macOS notification is raised naming the detected file

#### Scenario: Notification is raised whether or not the window is shown

- **WHEN** a file is detected while the main window is hidden
- **THEN** the notification is raised just as it would be with the window shown

### Requirement: Notification activation shows the application

Activating a notification SHALL bring the application's main window to the front, showing it first if it is hidden.

#### Scenario: Activation with the window hidden

- **WHEN** the user activates a notification while the main window is hidden
- **THEN** the window is shown and brought to the front

#### Scenario: Activation with the window already open

- **WHEN** the user activates a notification while the main window is open but not focused
- **THEN** the window is brought to the front

#### Scenario: Activation does not start a second instance

- **WHEN** a notification is activated
- **THEN** the existing process handles it and no additional process is started

### Requirement: Notification delivery failure is non-fatal

The application SHALL treat the inability to deliver a notification as a recoverable condition. A delivery failure SHALL be logged and SHALL NOT interrupt file detection, upload, or any other processing.

#### Scenario: Upload proceeds when notification delivery fails

- **WHEN** the platform refuses or silently drops a notification
- **THEN** the file is still detected, uploaded, and recorded in the log and counters

#### Scenario: Failure is recorded

- **WHEN** notification delivery fails
- **THEN** the failure is written to the log file at a severity that distinguishes it from normal operation

#### Scenario: Guaranteed feedback channels remain

- **WHEN** notifications cannot be delivered on a given platform
- **THEN** the in-app log viewer and the tray icon still reflect application activity

### Requirement: Stable application identity for notifications

The application SHALL declare a stable reverse-DNS bundle identifier, used by the macOS notification mechanism to attribute notifications to the application.

#### Scenario: Identifier is declared

- **WHEN** the macOS application bundle metadata is inspected
- **THEN** it declares a reverse-DNS bundle identifier

#### Scenario: Identifier is stable across releases

- **WHEN** two successive releases are compared
- **THEN** the bundle identifier is unchanged, so notification permissions granted by the user continue to apply
