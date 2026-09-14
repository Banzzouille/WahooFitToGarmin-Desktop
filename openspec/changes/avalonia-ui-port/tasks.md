## 1. Preconditions

- [ ] 1.1 Confirm `extract-platform-agnostic-core` is merged, so the watcher, upload pipeline, settings store, `INotifier`, and `IFolderPicker` abstractions exist in the core library
- [ ] 1.2 Confirm `garmin-di-oauth2-core` is merged, so the sign-in view, the verification code prompt, and the connection state exist and are known to be part of the porting surface; note that the password is entered but never stored, so no settings field holds it
- [ ] 1.3 Confirm `fit-device-emulation` is merged, so the device model and Unit ID settings exist and must appear on the ported settings page
- [ ] 1.4 Create branch `feat/avalonia-port`
- [ ] 1.5 Record the parity checklist from the WPF build: capture the current appearance and behaviour of the shell, main page, and settings page for side-by-side comparison

## 2. Project scaffolding

- [ ] 2.1 Create `WahooFitToGarmin.UI` targeting `net10.0` with no operating-system suffix, namespace `WahooFitToGarmin.UI`
- [ ] 2.2 Add Avalonia 12 and `Avalonia.Desktop`, pinning the exact version in `Directory.Packages.props`
- [ ] 2.2a Confirm no dependency requires Avalonia 12 specifically, so dropping to the 11.3 line remains a version bump rather than a redesign
- [ ] 2.3 Add FluentAvalonia
- [ ] 2.4 Add the cross-platform notification package
- [ ] 2.5 Reference the core library; do not reference the WPF project
- [ ] 2.6 Add the project to `WahooFitToGarmin-Desktop.sln`, leaving the WPF project in place for now
- [ ] 2.7 Verify an empty Avalonia window opens on Windows
- [ ] 2.8 Verify the same window opens on macOS

## 3. Composition root

- [ ] 3.1 Port the generic host wiring from `App.xaml.cs` to the Avalonia application lifetime
- [ ] 3.2 Set `ShutdownMode.OnExplicitShutdown`
- [ ] 3.3 Register the core services, view models, and views in the container
- [ ] 3.4 Port `ApplicationHostService` to create the main window without `IShellWindow`
- [ ] 3.5 Port the unhandled exception logging to Avalonia's equivalent hook
- [ ] 3.6 Implement the dispatcher abstraction over `Dispatcher.UIThread` and wire it into the log provider from `modernize-dotnet10-foundation`
- [ ] 3.7 Verify the bounded in-app log collection still receives entries, including entries emitted from the core library

## 4. Navigation

- [ ] 4.1 Implement `ViewLocator` as an `IDataTemplate` resolving views from view models by naming convention
- [ ] 4.2 Implement the reduced `INavigationService` over a `ContentControl`, preserving the back stack and the `Navigated` event
- [ ] 4.3 Build the shell window using FluentAvalonia's `NavigationView`, with the main page as a menu item and settings as an options item
- [ ] 4.4 Wire the back control and its enabled state
- [ ] 4.5 Port `ShellViewModel` to the new navigation service, replacing `HamburgerMenuItem` with the FluentAvalonia equivalent
- [ ] 4.6 Delete `PageService`, `FrameExtensions`, `MenuItemTemplateSelector`, and `IShellWindow` usages from the new project's design
- [ ] 4.7 Verify navigation between the two pages, and that the back control is disabled at startup

## 5. Theming

- [ ] 5.1 Implement the theme service mapping `AppTheme.Light`, `AppTheme.Dark`, and `AppTheme.Default` to `ThemeVariant.Light`, `ThemeVariant.Dark`, and `ThemeVariant.Default`
- [ ] 5.2 Apply the stored theme at startup, reading the existing `Theme` settings value without migration
- [ ] 5.3 Verify each theme applies immediately when selected
- [ ] 5.4 Verify the default variant follows the operating system appearance on Windows
- [ ] 5.5 Verify the default variant follows the operating system appearance on macOS
- [ ] 5.6 Verify the selected theme survives a restart

## 6. Resources

- [ ] 6.1 Move the strings currently hard-coded in `SettingsPage.xaml` into `Properties/Resources.resx` — "Keep uploaded activity file", "Garmin Info :", "Login", "Password", and any other literal
- [ ] 6.2 Add resource entries for the new tray menu items and device emulation labels
- [ ] 6.3 Verify resource lookup resolves from Avalonia markup on both operating systems
- [ ] 6.4 Verify no literal user-visible string remains in any view

## 7. Main page

- [ ] 7.1 Port the main page layout, including the page title
- [ ] 7.2 Port the log viewer with virtualization and the entry template binding `LogDateTime` and `LogMessage`
- [ ] 7.3 Bind the processed, failed, and duplicate counters, replacing the dead `{Binding Count}` placeholder
- [ ] 7.4 Port `MainViewModel` to the source-generated MVVM idiom
- [ ] 7.5 Verify log entries appear in chronological order with timestamps
- [ ] 7.6 Verify counters update as activities are processed

## 8. Settings page

- [ ] 8.1 Port the theme selection control
- [ ] 8.2 Implement `IFolderPicker` in the user interface project over `IStorageProvider.OpenFolderPickerAsync`, resolving the active window
- [ ] 8.3 Wire folder selection, displaying and persisting the chosen path
- [ ] 8.4 Port the keep-uploaded-file option
- [ ] 8.5 Port the device model selection and Unit ID fields from `fit-device-emulation`, including the guidance on where to find the Unit ID, the format-only validation feedback, and the text stating what the service does and does not compute
- [ ] 8.5a Review the ported emulation wording so that it still does not claim that enabling emulation produces recovery time
- [ ] 8.6 Port the version display and the repository link, opening the link through a cross-platform mechanism
- [ ] 8.7 Port `SettingsViewModel` to the source-generated MVVM idiom
- [ ] 8.8 Verify the folder picker opens natively on Windows and the path persists
- [ ] 8.9 Verify the folder picker opens natively on macOS and the path persists
- [ ] 8.10 Verify cancelling the picker leaves the configured folder unchanged
- [ ] 8.11 Verify the repository link opens the default browser on both operating systems

## 8b. Authentication views

- [ ] 8b.1 Port the sign-in view: email and password fields, sign-in action, connection state display
- [ ] 8b.2 Ensure the password field is never pre-filled, because nothing is stored
- [ ] 8b.3 Port the verification code prompt, stating the method the service used and not requiring the password to be re-entered
- [ ] 8b.4 Port the disconnect action
- [ ] 8b.5 Display the "sign in again required" state prominently when the pipeline is paused, and resume on successful sign-in
- [ ] 8b.6 Verify sign-in, code verification, disconnect, and the paused state on Windows
- [ ] 8b.7 Verify the same on macOS
- [ ] 8b.8 Verify that no password value appears in any log produced during these flows

## 9. Tray and lifecycle

- [ ] 9.1 Produce a monochrome template PNG asset for the macOS menu bar; keep the coloured icon for Windows
- [ ] 9.2 Add the `TrayIcon` with the per-platform asset
- [ ] 9.3 Add the tray menu with open and quit entries
- [ ] 9.4 Cancel the main window closing event and hide the window instead
- [ ] 9.5 Make quit the only path that terminates the process, persisting settings on the way out
- [ ] 9.6 Implement the single-instance guard: named mutex on Windows, lock file or local socket on macOS
- [ ] 9.7 Make the second instance signal the first to show its window, then exit
- [ ] 9.8 Release the guard on exit so a subsequent launch starts normally
- [ ] 9.9 Verify the tray icon appears on Windows and the menu works
- [ ] 9.10 Verify the menu bar icon appears on macOS and remains legible in light and dark appearance
- [ ] 9.11 Verify closing the window hides it and the process keeps running
- [ ] 9.12 Verify a file dropped while hidden is still detected and uploaded
- [ ] 9.13 Verify a second launch activates the first instance and uploads the next file only once

## 10. Notifications

- [ ] 10.1 Fix the reverse-DNS bundle identifier and declare it in the macOS bundle metadata
- [ ] 10.2 Implement `INotifier` over the cross-platform notification package
- [ ] 10.3 Catch, log, and swallow delivery failures so the pipeline is never interrupted
- [ ] 10.4 Implement notification activation: show the window if hidden and bring it to the front, without starting a second process
- [ ] 10.5 Delete `ToastNotificationsService`, its samples file, `IToastNotificationsService`, and rework `ToastNotificationActivationHandler` without WinRT
- [ ] 10.6 Verify a native notification is raised on Windows naming the detected file
- [ ] 10.7 Verify a native notification is raised on macOS naming the detected file
- [ ] 10.8 Test ad-hoc code signing on macOS and record whether notifications are delivered from an unsigned bundle
- [ ] 10.9 Verify the upload still completes when notification delivery fails, and that the failure is logged at an error severity

## 11. Parity verification on Windows

- [ ] 11.1 Run the Avalonia build against the same settings file as the WPF build
- [ ] 11.2 Verify settings load, including a settings file written by the previous version
- [ ] 11.3 Verify all three themes
- [ ] 11.4 Drop a real `.fit` file into the watched folder and verify detection, notification, upload, log entries, and counters
- [ ] 11.5 Verify the file is deleted when keep-uploaded-file is off, and retained when on
- [ ] 11.6 Compare the emitted log messages against the WPF build — same messages, same order
- [ ] 11.7 Walk the full parity checklist recorded in 1.5

## 12. Parity verification on macOS

- [ ] 12.1 Run against a real Dropbox folder on macOS
- [ ] 12.2 Verify `FileSystemWatcher` reliably reports Dropbox writes; if it does not, raise a polling fallback as follow-up work against the watcher in `extract-platform-agnostic-core`
- [ ] 12.3 Verify the file-stabilisation logic still prevents truncated uploads under macOS event behaviour
- [ ] 12.4 Verify the folder picker, theme switching, tray, and notifications
- [ ] 12.5 Verify settings and log files are written to the correct macOS user directories
- [ ] 12.6 Walk the full parity checklist recorded in 1.5

## 13. Remove WPF

- [ ] 13.1 Delete the `WahooFitToGarmin-Desktop` project directory in full, including all `.xaml` files, styles, themes, converters, template selectors, and `app.manifest`
- [ ] 13.2 Remove the project from `WahooFitToGarmin-Desktop.sln`
- [ ] 13.3 Remove `MahApps.Metro`, `Hardcodet.NotifyIcon.Wpf`, `Microsoft.Toolkit.Uwp.Notifications`, and `Microsoft.Xaml.Behaviors.Wpf` from `Directory.Packages.props`
- [ ] 13.4 Verify no project declares an operating-system-specific target framework
- [ ] 13.5 Verify no source file imports a Windows Runtime projection
- [ ] 13.6 Verify `dotnet build` succeeds on macOS for the whole solution
- [ ] 13.7 Verify `dotnet test` still passes

## 14. Documentation and follow-up

- [ ] 14.1 Update the README to state that closing the window hides the application to the tray
- [ ] 14.2 Update the README to record that the bundled high-contrast themes are removed and operating system accessibility settings apply instead
- [ ] 14.3 Note that the Windows autostart shortcut must be recreated because the executable name and layout changed
- [ ] 14.4 Record the answer to the macOS notification signing question for `cross-platform-packaging-ci`
- [ ] 14.5 Record any Avalonia-level defect encountered, rather than leaving a silent workaround in the code
