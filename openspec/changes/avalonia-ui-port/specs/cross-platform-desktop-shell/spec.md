## ADDED Requirements

### Requirement: Supported operating systems

The application SHALL run on Windows and on macOS from a single user interface codebase. A single user interface project SHALL serve both operating systems; there SHALL NOT be one project per platform.

#### Scenario: Application launches on Windows

- **WHEN** the application is launched on Windows 10 or later
- **THEN** the main window appears and the application is usable

#### Scenario: Application launches on macOS

- **WHEN** the application is launched on macOS 12 or later
- **THEN** the main window appears and the application is usable

#### Scenario: One user interface project serves both platforms

- **WHEN** the solution is inspected
- **THEN** exactly one user interface project exists, and its target framework carries no operating-system suffix

### Requirement: Main window with page navigation

The application SHALL present a main window containing a navigation menu that lists the available pages, with the settings entry separated from the main entries, and a back control that returns to the previously visited page.

#### Scenario: Pages are listed in the navigation menu

- **WHEN** the main window is shown
- **THEN** the navigation menu lists the main page, and lists settings as a separate options entry

#### Scenario: Selecting a menu entry navigates

- **WHEN** the user selects the settings entry
- **THEN** the settings page is displayed in the content area

#### Scenario: Back control returns to the previous page

- **WHEN** the user navigates from the main page to the settings page and then activates the back control
- **THEN** the main page is displayed again

#### Scenario: Back control is unavailable with no history

- **WHEN** the application has just started and no navigation has occurred
- **THEN** the back control is disabled

### Requirement: Theme selection

The application SHALL offer three theme choices — light, dark, and system default — on both operating systems. The selected theme SHALL be persisted and reapplied on the next launch. When system default is selected, the application SHALL follow the operating system's light or dark appearance.

#### Scenario: Light theme is applied

- **WHEN** the user selects the light theme on the settings page
- **THEN** the interface switches to light immediately

#### Scenario: Dark theme is applied

- **WHEN** the user selects the dark theme on the settings page
- **THEN** the interface switches to dark immediately

#### Scenario: System default follows the operating system

- **WHEN** the system default theme is selected and the operating system appearance changes between light and dark
- **THEN** the application follows that change

#### Scenario: Theme survives a restart

- **WHEN** a theme is selected and the application is restarted
- **THEN** the same theme is applied on startup

#### Scenario: Stored theme values remain compatible

- **WHEN** a settings file written by a previous version contains a stored theme value
- **THEN** that value is understood and applied without migration

### Requirement: Native folder selection

The application SHALL let the user choose the watched folder through the operating system's native folder picker on both platforms. The chosen path SHALL be displayed and persisted.

#### Scenario: Folder picker opens on Windows

- **WHEN** the user activates folder selection on Windows
- **THEN** the native Windows folder picker opens

#### Scenario: Folder picker opens on macOS

- **WHEN** the user activates folder selection on macOS
- **THEN** the native macOS folder picker opens

#### Scenario: Chosen folder is persisted

- **WHEN** the user selects a folder and the picker is confirmed
- **THEN** the path is displayed on the settings page and persisted to settings

#### Scenario: Cancelling leaves the setting unchanged

- **WHEN** the user opens the folder picker and cancels it
- **THEN** the previously configured folder remains unchanged

### Requirement: Main page displays activity

The main page SHALL display the running log of application activity, most recent entries visible, together with the counters for processed, failed, and duplicate activities.

#### Scenario: Log entries are displayed

- **WHEN** the application emits log messages
- **THEN** they appear on the main page in chronological order with their timestamp

#### Scenario: Counters are displayed

- **WHEN** activities have been processed, have failed, or were rejected as duplicates
- **THEN** the corresponding counts are visible on the main page

### Requirement: Settings page content

The settings page SHALL expose the watched folder, the keep-uploaded-file option, theme selection, the application version, and a link to the project repository. Activating the repository link SHALL open it in the user's default browser on both operating systems.

#### Scenario: Keep-uploaded-file option is togglable and persisted

- **WHEN** the user toggles the keep-uploaded-file option and restarts the application
- **THEN** the option retains the value chosen

#### Scenario: Version is displayed

- **WHEN** the settings page is shown
- **THEN** a non-empty application version is displayed

#### Scenario: Repository link opens in the default browser

- **WHEN** the user activates the repository link on either operating system
- **THEN** the project page opens in the default browser and the application keeps running

### Requirement: User-visible strings come from resources

User-visible text in views SHALL be supplied from the resource files rather than hard-coded in markup, so that the interface can be translated without editing views.

#### Scenario: No hard-coded user-visible text remains

- **WHEN** the view markup is inspected
- **THEN** no literal user-visible string is present, including the labels that were hard-coded in the previous settings page

#### Scenario: Resource lookup resolves on both platforms

- **WHEN** the application runs on Windows and on macOS
- **THEN** all labels render with their resource text rather than a missing-resource placeholder
