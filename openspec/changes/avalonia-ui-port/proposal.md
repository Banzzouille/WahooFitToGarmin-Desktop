## Why

The application is WPF, which runs only on Windows, so macOS users have no way to run it at all. WPF cannot be made cross-platform by configuration — the user interface layer has to be rebuilt on a framework that renders on both operating systems. This is the change that actually delivers macOS support; every change before it prepares the ground.

## What Changes

- Replace the WPF user interface with Avalonia. A new desktop project is added, and `WahooFitToGarmin-Desktop` (WPF) is deleted once feature parity is reached. **BREAKING** at the packaging level: the produced binary, its target framework, and its launch mechanism all change.
- The application runs on Windows and macOS from a single codebase and a single user interface project.
- Replace MahApps.Metro with FluentAvalonia. The Light, Dark, and system-default theme choices are preserved.
- **BREAKING**: the two custom high-contrast theme dictionaries (`Styles/Themes/HC.Dark.Blue.xaml`, `Styles/Themes/HC.Light.Blue.xaml`) are removed. High-contrast accessibility is delegated to the operating system rather than to bundled theme dictionaries.
- Replace the WPF `Frame`-based navigation stack — `NavigationService`, `PageService`, `FrameExtensions`, `IShellWindow`, `MenuItemTemplateSelector` — with a view-locator approach and a FluentAvalonia navigation control. The hamburger menu, the page list, and the back button keep their current behaviour.
- Replace `System.Windows.Forms.FolderBrowserDialog` with Avalonia's storage provider, so folder selection works natively on both operating systems.
- Replace the UWP toast notification stack (`Windows.UI.Notifications`, `Windows.Data.Xml.Dom`, `Microsoft.Toolkit.Uwp.Notifications`) with notifications that are native on both platforms. Clicking a notification still brings the window forward, as it does today.
- Add a tray icon on Windows and a menu bar item on macOS. `Hardcodet.NotifyIcon.Wpf` is referenced today but never used — there is no tray icon in the current application. **BREAKING**: closing the window now hides the application to the tray and file watching continues, instead of terminating the process. Quitting is done from the tray menu.
- Remove `app.manifest`, `UseWindowsForms`, the Windows-specific target framework moniker, and every `.xaml` file belonging to the WPF project.
- Rewrite the view models against Avalonia's binding and command model, adopting the `CommunityToolkit.Mvvm` source generators that `modernize-dotnet10-foundation` deliberately deferred.

Preserved without behavioural change: the watched-folder pipeline, the keep-uploaded-file option, the in-app log viewer and its bounded collection, log file location and rotation, settings persistence, the displayed version, the GitHub link, and the Garmin upload path.

This change depends on `extract-platform-agnostic-core` having moved application logic out of the view models, and on `garmin-di-oauth2-core` having replaced the authentication flow. It does not itself change what the application does — only where and how it draws.

## Capabilities

### New Capabilities

- `cross-platform-desktop-shell`: which operating systems the application runs on, and the behaviour of the window, navigation, pages, theme selection, and folder selection on each of them.
- `background-tray-operation`: the tray or menu bar presence, what closing the window does, what the tray menu offers, and the guarantee that file watching continues while no window is shown.
- `desktop-notifications`: how the user is notified of activity on each operating system, and what happens when a notification is activated.

### Modified Capabilities

- `runtime-platform`: the requirement that the desktop project declares a Windows-specific target framework is removed, and the requirement that the solution contains no Windows-only dependency is extended from the core library to the whole solution. Assumes `modernize-dotnet10-foundation` has been archived and its specs promoted to `openspec/specs/`.

## Impact

**Projects**
- New: `WahooFitToGarmin.UI` (Avalonia, `net10.0`)
- Deleted: `WahooFitToGarmin-Desktop` (WPF) and everything under it
- `WahooFitToGarmin-Desktop.sln` — project list updated

**Deleted source**
- All ten `.xaml` files: `App.xaml`, `Views/ShellWindow.xaml`, `Views/MainPage.xaml`, `Views/SettingsPage.xaml`, `Styles/*.xaml`, `Styles/Themes/*.xaml`
- `Services/NavigationService.cs`, `Services/PageService.cs`, `Services/ThemeSelectorService.cs`, `Services/ToastNotificationsService.cs`, `Services/ToastNotificationsService.Samples.cs`, `Services/SystemService.cs`
- `Helpers/FrameExtensions.cs`, `Helpers/PropertyChangedBase.cs`, `TemplateSelectors/MenuItemTemplateSelector.cs`, `Converters/EnumToBooleanConverter.cs`
- `Contracts/Views/IShellWindow.cs`, `Contracts/Services/INavigationService.cs`, `Contracts/Services/IPageService.cs`, `Contracts/Services/IToastNotificationsService.cs`
- `app.manifest`

**Rewritten source**
- `App.xaml.cs` — Avalonia application lifetime and composition root
- `Services/ApplicationHostService.cs` — window creation without `IShellWindow`
- `ViewModels/ShellViewModel.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/SettingsViewModel.cs`
- `Activation/ToastNotificationActivationHandler.cs` — notification activation without WinRT

**Dependencies**
- Removed: `MahApps.Metro`, `Hardcodet.NotifyIcon.Wpf`, `Microsoft.Toolkit.Uwp.Notifications`, `Microsoft.Xaml.Behaviors.Wpf`
- Added: `Avalonia`, `Avalonia.Desktop`, `FluentAvalonia`, a cross-platform notification package
- Unchanged: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, Serilog, and everything in the core library

**Users**
- macOS becomes a supported platform.
- Closing the window no longer quits the application.
- Bundled high-contrast themes are gone; the operating system's accessibility settings apply instead.
- The Windows autostart shortcut target changes, since the executable name and layout change.

**Downstream changes**
Unblocks `cross-platform-packaging-ci`, which packages the macOS application bundle. The previously planned `webview-login-capture` change is cancelled: `garmin-di-oauth2-core` signs in programmatically, so there is no ticket for a web view to capture.
