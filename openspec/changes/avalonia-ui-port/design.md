## Context

By the time this change starts, `extract-platform-agnostic-core` has moved the watcher, upload pipeline, settings store, and notification abstraction into the core library, and `garmin-di-oauth2-core` has replaced the authentication flow. What remains in the desktop project is genuinely presentation: three view models, four XAML views, six styling dictionaries, and the Windows-Template-Studio navigation plumbing.

That is the entire porting surface, and it is small — roughly 600 lines of C# and ten XAML files, of which most is template boilerplate that is deleted rather than translated. The application has two pages.

Two constraints shape the design:

1. **The view surface grew after this change was first designed.** `garmin-di-oauth2-core` adds a sign-in view and a verification code prompt, and `fit-device-emulation` adds a device selector and a Unit ID field to the settings page. All of them are written against WPF and must be ported here.
2. **`cross-platform-packaging-ci` ships an unsigned macOS bundle.** Anything in the user interface that depends on code signing to function is a problem that has to be identified here, not discovered at packaging time.

## Goals / Non-Goals

**Goals:**
- One user interface project that builds and runs on Windows and macOS.
- Feature parity with the WPF application, verified item by item against the preserved-behaviour list in the proposal.
- Tray presence on both platforms, with file watching continuing while no window is shown.
- Notifications that work on both platforms, degrading safely when the platform refuses to deliver them.
- No WPF, Windows Forms, or WinRT reference anywhere in the solution when the change is complete.

**Non-Goals:**
- Linux support. Avalonia would give it nearly for free, but it is untested here and claiming it without testing is worse than not claiming it.
- Visual redesign. The layout, page structure, and wording stay as they are.
- Renaming the core library's `WahooFitToGarmin_Desktop.Core` namespace. It is ugly, and touching it would produce a diff that swamps the actual port.
- Localisation beyond the existing English resources.
- Window position and size persistence. It does not exist today.

## Decisions

### D1 — Native Avalonia port, not Avalonia XPF

XPF runs existing WPF XAML on Avalonia's renderer with minimal code changes, which is the right answer for a large WPF codebase with deep control-suite dependencies. This codebase is two pages of XAML plus template scaffolding scheduled for deletion. A native port costs about the same effort and produces a codebase with no commercial per-developer licence attached, which matters for an open-source project where contributors cannot be assumed to hold one.

*Alternative considered:* .NET MAUI. Its macOS story is Mac Catalyst, which is an iOS-app-on-macOS compatibility layer rather than a native desktop framework, and its desktop tray and window management support is weaker than Avalonia's.

### D2 — Avalonia 12, pinned, and now genuinely reversible

An earlier draft of this design pinned Avalonia 12 because `Avalonia.Controls.WebView` requires it and the planned `webview-login-capture` change could not proceed otherwise. That constraint no longer exists: sign-in is programmatic, no web view is embedded, and that change is cancelled.

Avalonia 12 remains the choice, but on merit rather than under obligation — it is the current line, where documentation, fixes, and platform work are landing. The version is pinned exactly in `Directory.Packages.props`, and an Avalonia-level defect is a reason to reassess rather than to work around silently.

What has changed is that the retreat is now real. Nothing in the solution depends on a package that exists only for Avalonia 12, so dropping to the 11.3 line costs a version bump and a compile, not a redesign. That should be verified rather than assumed: if any dependency turns out to require 12, it is worth knowing before the port is complete.

### D3 — New project, WPF deleted at the end of the same branch

`WahooFitToGarmin.UI`, targeting `net10.0` with no OS suffix, is added alongside the WPF project. Both exist during the port so behaviour can be compared side by side. The WPF project is deleted in the final commit of the branch, once the parity checklist passes.

The new project uses the namespace `WahooFitToGarmin.UI`. The core library keeps `WahooFitToGarmin_Desktop.Core` (see Non-Goals).

### D4 — Theming: FluentAvalonia, three variants, high contrast delegated to the operating system

`AppTheme.Light`, `AppTheme.Dark`, and `AppTheme.Default` map to Avalonia's `ThemeVariant.Light`, `ThemeVariant.Dark`, and `ThemeVariant.Default`, where Default follows the operating system. That is the same three-way choice the settings page offers today, so the radio buttons and the stored `Theme` setting value are unchanged.

MahApps' `ThemeSyncMode.SyncWithHighContrast` and the two bundled high-contrast dictionaries are dropped. They were template stubs — the file comment in `ThemeSelectorService` says as much, describing them as basic dictionaries the developer is expected to complete. Reimplementing incomplete accessibility theming on a new framework would be inventing a feature, not porting one. Operating-system-level high contrast and accessibility settings apply instead.

### D5 — Navigation: view-locator with a thin service, keeping the view-model-first shape

The current design navigates by view-model type name: `ShellViewModel` holds `HamburgerMenuItem`s whose `TargetPageType` is a view model type, `NavigationService.NavigateTo` takes the type's full name, and `PageService` maps that name to a page type.

Avalonia's idiomatic equivalent is a `ViewLocator` implementing `IDataTemplate`, which resolves a view from a view model instance by naming convention. `PageService` and `FrameExtensions` disappear entirely. A much smaller `INavigationService` is kept over a `ContentControl` — it preserves the back stack and the `Navigated` event that `ShellViewModel` subscribes to, so the shell's logic ports rather than being rewritten.

FluentAvalonia's `NavigationView` replaces MahApps' `HamburgerMenu`, with the same split between main items and options items that puts Settings at the bottom.

### D6 — Tray: Avalonia `TrayIcon`, close hides, quit is explicit

Avalonia's built-in `TrayIcon` covers the Windows notification area and the macOS status bar, so no third-party package is needed — `Hardcodet.NotifyIcon.Wpf` is simply removed rather than replaced.

The application lifetime uses `ShutdownMode.OnExplicitShutdown`. The main window's closing event is cancelled and the window hidden instead. The tray menu offers Open and Quit; Quit is the only path that terminates the process.

macOS requires a monochrome template image for the status bar; the existing coloured `.ico` and `.png` assets will render badly. A monochrome PNG asset is added for the macOS menu bar, with the coloured icon retained for Windows.

The macOS Dock icon follows the window rather than staying put. An earlier
version of this decision kept it permanently, on the grounds that hiding it meant
marking the bundle as a user interface element and that was not worth the
complexity. That was wrong on both counts, and it was wrong in a way a user
noticed immediately: an application that has been closed to the menu bar has no
business still occupying the Dock.

The static property is indeed the wrong tool — it is all or nothing, so the icon
would never appear, not even while someone has the window open and is working in
it. The right tool is the activation policy, switched at runtime: an ordinary
application while the window is shown, a background utility once it is closed.
That is about thirty lines of interop, not the complexity the original decision
imagined.

Verified on macOS: the system reports the process as `Foreground` with the window
open and `UIElement` once it is closed, across repeated cycles, with the process
still running and still watching throughout.

### D7 — Single-instance guard, introduced because close-to-tray makes it necessary

Today, closing the window terminates the process, so a second launch is a fresh start. Once closing only hides the window, a user who launches the application again — from the Start menu, from the Dock, from an autostart entry — gets a second process watching the same folder. Two watchers means two uploads of the same file, and Garmin's duplicate handling is the only thing preventing visible damage.

A single-instance guard is therefore part of this change, not a nice-to-have: a named mutex on Windows and a lock file or local socket on macOS, with the second instance signalling the first to show its window and then exiting.

### D8 — Notifications through a platform abstraction with a guaranteed fallback

`INotifier` is defined in the core library by `extract-platform-agnostic-core`. This change supplies the implementation. A cross-platform desktop notification package provides native toasts on Windows and `UNUserNotificationCenter` notifications on macOS.

The macOS path has a dependency the Windows path does not: notification delivery is tied to the application bundle's identity, and an unsigned bundle may have its notifications silently dropped. Since `cross-platform-packaging-ci` deliberately ships unsigned, the implementation must not assume delivery. Two consequences:

- A bundle identifier is fixed now, in reverse-DNS form, because the notification backend needs one.
- Notification failure is caught, logged, and swallowed. The tray icon and the in-app log viewer remain the guaranteed feedback channels; a missing notification degrades the experience but never breaks the pipeline.

Ad-hoc code signing, which is free and requires no Apple Developer account, is the first mitigation to test on real hardware.

### D9 — Folder selection through the storage provider, bridged by a window-aware service

`IStorageProvider.OpenFolderPickerAsync` is reached from a `TopLevel`, which means the picker needs a window reference — something a view model in the core library must not have. The `IFolderPicker` abstraction is implemented in the user interface project, where it resolves the active window and calls the storage provider. This replaces `System.Windows.Forms.FolderBrowserDialog` and is the last reason the Windows Forms reference exists.

### D10 — The logging provider's dispatcher becomes an abstraction

`modernize-dotnet10-foundation` introduced an `ILoggerProvider` that marshals log entries onto the user interface thread via `Application.Current.Dispatcher`, a WPF API. Avalonia's equivalent is `Dispatcher.UIThread`. The provider is reworked to depend on a small dispatcher abstraction, with the Avalonia implementation supplied by the user interface project, so the bounded in-app log viewer behaviour carries over unchanged.

### D11 — View models adopt the MVVM source generators here

`modernize-dotnet10-foundation` deliberately deferred `[ObservableProperty]` and `[RelayCommand]` to avoid rewriting view models that were about to be rewritten. This is that rewrite, so the deferral ends. The three view models are re-expressed with generators as they are ported.

### D12 — Existing `.resx` resources are kept

`Properties/Resources.resx` works through the standard .NET resource manager, which is framework-agnostic. Strings are referenced from Avalonia XAML the same way they were from WPF XAML. There is no reason to migrate to another localisation mechanism as part of a framework port.

## Risks / Trade-offs

**macOS notifications may never appear from an unsigned bundle** → Designed around rather than gambled on, per D8: failures are swallowed, and the tray plus log viewer are the guaranteed channels. Ad-hoc signing is tested first. If notifications prove undeliverable unsigned, that becomes a documented macOS limitation rather than a blocker, and an input to reconsidering the signing decision in `cross-platform-packaging-ci`.

**`FileSystemWatcher` behaves differently on macOS** → The .NET implementation maps to FSEvents and kqueue rather than the Win32 API, with documented differences in event coalescing and descriptor limits, and Dropbox's write pattern is already the source of the truncated-upload bug that `extract-platform-agnostic-core` fixes. The file-stabilisation logic introduced there is what makes this survivable, but it was written and tested against Windows behaviour only. A real Dropbox folder on real macOS must be part of this change's verification, with a polling fallback as the contingency if event delivery proves unreliable.

**Avalonia 12 is a recent major release** → Version pinned; an Avalonia-level defect is escalated rather than worked around. The 11.3 line is a genuine retreat now that nothing depends on a version-12-only package, which is itself worth confirming early rather than at the moment it is needed.

**FluentAvalonia's `NavigationView` will not map one-to-one onto MahApps' `HamburgerMenu`** → The shell is roughly 100 lines of view model and one view. Accepting FluentAvalonia's idioms, rather than forcing MahApps' structure onto it, is cheaper and produces a more native result. The user-visible contract is only: a menu listing pages, Settings separated at the bottom, a working back button.

**The macOS status bar icon will look wrong if the Windows icon is reused** → A monochrome template asset is produced as part of the work, not left to packaging.

**No drag-and-drop visual designer** → Avalonia offers a live previewer in Visual Studio, VS Code, and Rider. For two pages this is not a meaningful cost.

**Regression risk from rewriting all three view models at once** → The WPF project stays in the solution until the parity checklist passes, so the old and new user interfaces can be run side by side against the same settings file and the same watched folder.

## Migration Plan

Branch `feat/avalonia-port`, merged as one unit.

1. Add `WahooFitToGarmin.UI` with Avalonia 12 pinned, FluentAvalonia, and the notification package. Confirm that no dependency forces version 12, so the 11.3 retreat stays available. Both user interface projects coexist.
2. Composition root: Avalonia application lifetime, generic host wiring, `ShutdownMode.OnExplicitShutdown`.
3. `ViewLocator`, the reduced `INavigationService`, and the shell window with `NavigationView`.
4. Port the main page, including the log viewer and the counters from `extract-platform-agnostic-core`.
5. Port the settings page: theme, folder picker, keep-uploaded-file, device selection and Unit ID from `fit-device-emulation`, version, GitHub link.
6. Port the authentication views from `garmin-di-oauth2-core`: sign-in, verification code prompt, connection state, disconnect, and the prominent "sign in again required" state.
7. Theme service against `ThemeVariant`.
8. Tray icon, close-to-hide, tray menu, single-instance guard, monochrome macOS asset.
9. Notifier implementation with failure swallowing; activation brings the window forward.
10. Dispatcher abstraction for the log provider.
11. Parity checklist on Windows, against the WPF build running the same settings.
12. Parity checklist on macOS, including a real Dropbox folder.
13. Delete the WPF project, its files, and the now-unused packages. Update the solution.
14. Update the README: supported platforms, macOS instructions, close-to-tray behaviour, high-contrast removal.

**Rollback:** revert the branch. No stored data format changes, so a rolled-back installation reads the same settings file. The Windows autostart shortcut is the one external artefact that changes; the README documents recreating it.

## Open Questions

- Does an ad-hoc signed, unsigned-for-distribution macOS bundle actually deliver notifications? Answered on real hardware at step 8, before the implementation is considered complete.
- Does Avalonia's `TrayIcon` require the application to be launched from a `.app` bundle on macOS, or does it work from a bare executable during development? Affects how early step 7 can be tested.
- Does `FileSystemWatcher` reliably report Dropbox writes on macOS, or is polling required? Answered at step 11; the answer may add a task to `extract-platform-agnostic-core`'s watcher.
- Is the existing `.resx` string set complete enough to drive both platforms, given that several strings are currently hard-coded in `SettingsPage.xaml` — "Keep uploaded activity file", "Garmin Info :", "Login", "Password"? These should move into resources during the port rather than being hard-coded a second time.
