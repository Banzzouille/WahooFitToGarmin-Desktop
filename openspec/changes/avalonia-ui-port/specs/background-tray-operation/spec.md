## ADDED Requirements

### Requirement: Tray presence on both platforms

The application SHALL display an icon in the Windows notification area and in the macOS menu bar while it is running. The macOS icon SHALL use a monochrome template asset suited to the menu bar; the Windows icon SHALL use the coloured application icon.

#### Scenario: Icon appears on Windows

- **WHEN** the application is running on Windows
- **THEN** its icon is present in the notification area

#### Scenario: Icon appears on macOS

- **WHEN** the application is running on macOS
- **THEN** its icon is present in the menu bar

#### Scenario: macOS icon is legible in both appearances

- **WHEN** the macOS appearance is switched between light and dark
- **THEN** the menu bar icon remains legible, because it is a monochrome template image rather than the coloured application icon

### Requirement: Closing the window hides the application

Closing the main window SHALL hide the application rather than terminate it. The application SHALL continue watching the configured folder while no window is shown.

#### Scenario: Closing hides rather than quits

- **WHEN** the user closes the main window
- **THEN** the window disappears, the process keeps running, and the tray icon remains present

#### Scenario: Watching continues while hidden

- **WHEN** the window is hidden and a `.fit` file appears in the watched folder
- **THEN** the file is detected and uploaded exactly as it would be with the window shown

#### Scenario: Activity while hidden is recorded

- **WHEN** activity occurs while the window is hidden and the window is later shown
- **THEN** the log entries and counters produced during that period are visible

### Requirement: Tray menu offers open and quit

The tray icon SHALL provide a menu with at least an entry that shows the main window and an entry that quits the application. Quitting from that menu SHALL terminate the process.

#### Scenario: Open shows the window

- **WHEN** the user selects the open entry from the tray menu while the window is hidden
- **THEN** the main window is shown and brought to the front

#### Scenario: Quit terminates the application

- **WHEN** the user selects the quit entry from the tray menu
- **THEN** the process exits, settings are persisted, and the tray icon disappears

#### Scenario: Quit is the only termination path from the interface

- **WHEN** the user closes the window rather than selecting quit
- **THEN** the process does not exit

### Requirement: Single running instance

Only one instance of the application SHALL watch the configured folder at a time. Launching the application while an instance is already running SHALL activate the existing instance and terminate the newly launched one.

#### Scenario: Second launch activates the first instance

- **WHEN** the application is already running with its window hidden and the user launches it again
- **THEN** the existing instance shows its window and the newly launched process exits

#### Scenario: No duplicate watching

- **WHEN** a second launch has been attempted and a `.fit` file then appears in the watched folder
- **THEN** the file is uploaded once, not twice

#### Scenario: Guard is released on exit

- **WHEN** the running instance quits and the application is launched again
- **THEN** the new instance starts normally as the sole instance
