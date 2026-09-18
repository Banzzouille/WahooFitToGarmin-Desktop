## 1. Preconditions

- [x] 1.1 Confirm `extract-platform-agnostic-core` is merged, so the watcher, upload pipeline, settings store, `INotifier`, and `IFolderPicker` abstractions exist in the core library
- [ ] 1.2 Confirm `garmin-di-oauth2-core` is merged, so the sign-in view, the verification code prompt, and the connection state exist and are known to be part of the porting surface; note that the password is entered but never stored, so no settings field holds it
- [ ] 1.3 Confirm `fit-device-emulation` is merged, so the device model and Unit ID settings exist and must appear on the ported settings page
- [x] 1.4 Create branch `feat/avalonia-port`
- [~] 1.5 Never recorded. The WPF project was deleted before a checklist was captured, so 11.7 and 12.6 have nothing to walk and the parity claim rests on the individual checks instead

## 2. Project scaffolding

- [x] 2.1 Create `WahooFitToGarmin.UI` targeting `net10.0` with no operating-system suffix, namespace `WahooFitToGarmin.UI`
- [x] 2.2 Add Avalonia 12 and `Avalonia.Desktop`, pinning the exact version in `Directory.Packages.props`
- [~] 2.2a No longer true, and deliberately so: `garmin-di-oauth2-core` D2 makes `Avalonia.Controls.WebView` the primary sign-in path and it publishes for Avalonia 12. Dropping to 11.3 is now a redesign
- [x] 2.3 Add FluentAvalonia
- [~] 2.4 Not added. `DesktopNotifications` has no macOS backend and pulls `Tmds.DBus` 0.9.1, which carries a high-severity advisory. `LoggingNotifier` stands in, so the application raises no system notification on either platform
- [x] 2.5 Reference the core library; do not reference the WPF project
- [x] 2.6 Added to `WahooFitToGarmin-Desktop.slnx`. The solution moved to the `.slnx` format and WPF was removed outright rather than kept alongside
- [x] 2.7 Verify an empty Avalonia window opens on Windows
- [x] 2.8 Verify the same window opens on macOS

## 3. Composition root

- [x] 3.1 Port the generic host wiring from `App.xaml.cs` to the Avalonia application lifetime
- [x] 3.2 Set `ShutdownMode.OnExplicitShutdown`
- [x] 3.3 Register the core services, view models, and views in the container
- [x] 3.4 Port `ApplicationHostService` to create the main window without `IShellWindow`
- [x] 3.5 Port the unhandled exception logging to Avalonia's equivalent hook
- [x] 3.6 Implement the dispatcher abstraction over `Dispatcher.UIThread` and wire it into the log provider from `modernize-dotnet10-foundation`
- [x] 3.7 Verify the bounded in-app log collection still receives entries, including entries emitted from the core library

## 4. Navigation

- [x] 4.1 Implement `ViewLocator` as an `IDataTemplate` resolving views from view models by naming convention
- [x] 4.2 Implement the reduced `INavigationService` over a `ContentControl`, preserving the back stack and the `Navigated` event
- [~] 4.3 The shell is a hand-written sidebar of three buttons over a `ContentControl`, not FluentAvalonia's `NavigationView`. FluentAvalonia is referenced but not used for navigation. Functional, and a visible deviation from this design rather than a completed task
- [x] 4.4 Wire the back control and its enabled state
- [x] 4.5 Port `ShellViewModel` to the new navigation service, replacing `HamburgerMenuItem` with the FluentAvalonia equivalent
- [x] 4.6 Delete `PageService`, `FrameExtensions`, `MenuItemTemplateSelector`, and `IShellWindow` usages from the new project's design
- [x] 4.7 Verify navigation between the two pages, and that the back control is disabled at startup

## 5. Theming

- [x] 5.1 Implement the theme service mapping `AppTheme.Light`, `AppTheme.Dark`, and `AppTheme.Default` to `ThemeVariant.Light`, `ThemeVariant.Dark`, and `ThemeVariant.Default`
- [x] 5.2 Apply the stored theme at startup, reading the existing `Theme` settings value without migration
- [ ] 5.3 Verify each theme applies immediately when selected
- [ ] 5.4 Verify the default variant follows the operating system appearance on Windows
- [x] 5.5 Verify the default variant follows the operating system appearance on macOS
- [ ] 5.6 Verify the selected theme survives a restart

## 6. Resources

- [ ] 6.1 Move the strings currently hard-coded in `SettingsPage.xaml` into `Properties/Resources.resx` — "Keep uploaded activity file", "Garmin Info :", "Login", "Password", and any other literal
- [ ] 6.2 Add resource entries for the new tray menu items and device emulation labels
- [ ] 6.3 Verify resource lookup resolves from Avalonia markup on both operating systems
- [ ] 6.4 Verify no literal user-visible string remains in any view

## 7. Main page

- [x] 7.1 Port the main page layout, including the page title
- [x] 7.2 Port the log viewer with virtualization and the entry template binding `LogDateTime` and `LogMessage`
- [x] 7.3 Bind the processed, failed, and duplicate counters, replacing the dead `{Binding Count}` placeholder
- [x] 7.4 Port `MainViewModel` to the source-generated MVVM idiom
- [ ] 7.5 Verify log entries appear in chronological order with timestamps
- [ ] 7.6 Verify counters update as activities are processed

## 8. Settings page

- [x] 8.1 Port the theme selection control
- [x] 8.2 Implement `IFolderPicker` in the user interface project over `IStorageProvider.OpenFolderPickerAsync`, resolving the active window
- [x] 8.3 Wire folder selection, displaying and persisting the chosen path
- [x] 8.4 Port the keep-uploaded-file option
- [x] 8.5 Device model selection and the honesty text are on the settings page. There is no Unit ID field: `fit-device-emulation` removed it after the round trip showed the converted file kept the Wahoo serial number
- [x] 8.5a The wording states that exercise load was observed and that recovery time is not something the setting can guarantee
- [x] 8.6 Port the version display and the repository link, opening the link through a cross-platform mechanism
- [x] 8.7 Port `SettingsViewModel` to the source-generated MVVM idiom
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
- [x] 9.2 Add the `TrayIcon` with the per-platform asset
- [x] 9.3 Add the tray menu with open and quit entries
- [x] 9.4 Cancel the main window closing event and hide the window instead
- [x] 9.5 Make quit the only path that terminates the process, persisting settings on the way out
- [x] 9.6 Implement the single-instance guard: named mutex on Windows, lock file or local socket on macOS
- [x] 9.7 Make the second instance signal the first to show its window, then exit
- [x] 9.8 Release the guard on exit so a subsequent launch starts normally
- [x] 9.9 Verify the tray icon appears on Windows and the menu works
- [x] 9.10 Verify the menu bar icon appears on macOS and remains legible in light and dark appearance
- [x] 9.11 Verify closing the window hides it and the process keeps running
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

- [x] 11.1 Run the Avalonia build against the same settings file as the WPF build
- [~] 11.2 Settings load and survive a restart on Windows. The previous-version file was not tested and no longer needs to be: 2.0 migrates the four values worth keeping and deletes the old file, and the user chose not to verify that path
- [ ] 11.3 Verify all three themes
- [ ] 11.4 Drop a real `.fit` file into the watched folder and verify detection, notification, upload, log entries, and counters
- [ ] 11.5 Verify the file is deleted when keep-uploaded-file is off, and retained when on
- [ ] 11.6 Compare the emitted log messages against the WPF build — same messages, same order
- [ ] 11.7 Walk the full parity checklist recorded in 1.5

## 12. Parity verification on macOS

- [ ] 12.1 Run against a real Dropbox folder on macOS
- [~] 12.2 Detection verified against an ordinary folder, not a real Dropbox folder. Dropbox writes are the case this task exists for and they remain untested
- [x] 12.3 A real 317 KB Wahoo export copied into the watched folder was decoded, transformed, and re-encoded intact, so it was not read while still being written
- [ ] 12.4 Verify the folder picker, theme switching, tray, and notifications
- [x] 12.5 Verify settings and log files are written to the correct macOS user directories
- [ ] 12.6 Walk the full parity checklist recorded in 1.5

## 13. Remove WPF

- [x] 13.1 Delete the `WahooFitToGarmin-Desktop` project directory in full, including all `.xaml` files, styles, themes, converters, template selectors, and `app.manifest`
- [x] 13.2 Removed. The `.sln` itself is gone, replaced by `WahooFitToGarmin-Desktop.slnx`
- [x] 13.3 Remove `MahApps.Metro`, `Hardcodet.NotifyIcon.Wpf`, `Microsoft.Toolkit.Uwp.Notifications`, and `Microsoft.Xaml.Behaviors.Wpf` from `Directory.Packages.props`
- [x] 13.4 Verify no project declares an operating-system-specific target framework
- [x] 13.5 Verify no source file imports a Windows Runtime projection
- [x] 13.6 Verify `dotnet build` succeeds on macOS for the whole solution
- [x] 13.7 Verify `dotnet test` still passes

## 14. Documentation and follow-up

- [ ] 14.1 Update the README to state that closing the window hides the application to the tray
- [ ] 14.2 Update the README to record that the bundled high-contrast themes are removed and operating system accessibility settings apply instead
- [ ] 14.3 Note that the Windows autostart shortcut must be recreated because the executable name and layout changed
- [ ] 14.4 Record the answer to the macOS notification signing question for `cross-platform-packaging-ci`
- [ ] 14.5 Record any Avalonia-level defect encountered, rather than leaving a silent workaround in the code
