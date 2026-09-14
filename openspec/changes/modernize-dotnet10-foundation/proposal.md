## Why

The application targets `netcoreapp3.1`, which reached end of life in December 2022, and depends on packages that are deprecated (`Microsoft.Toolkit.Mvvm` 7.1.2) or out of support (`Microsoft.Extensions.Hosting` 6.0.1). No test project and no CI exist, so every subsequent change — cross-platform support, the Garmin authentication rewrite, FIT device emulation — would be built on an unverifiable foundation. This change establishes a supported, testable runtime baseline before any functional work begins.

## What Changes

- Retarget both projects to .NET 10 (LTS): `WahooFitToGarmin-Desktop.Core` from `netstandard2.0`, `WahooFitToGarmin-Desktop` from `netcoreapp3.1`. The UI project stays WPF/Windows-only at this stage; cross-platform support is a later change.
- Enable `Nullable`, `ImplicitUsings`, and `LangVersion latest` on both projects.
- Introduce `Directory.Packages.props` for central package version management.
- Migrate `Microsoft.Toolkit.Mvvm` 7.1.2 to `CommunityToolkit.Mvvm` 8.x, including the `Microsoft.Toolkit.Mvvm.*` to `CommunityToolkit.Mvvm.*` namespace change.
- Upgrade `Microsoft.Extensions.Hosting` to 10.x.
- Replace `Newtonsoft.Json` with `System.Text.Json` in `Core/Services/FileService.cs`. **BREAKING** at the storage layer: the persisted settings file is read by both serializers during a transition window so existing installs are not lost.
- Replace the current logging mechanism. Today `Helpers/LogEntry.cs` performs a synchronous `File.AppendText` inside its constructor, on the UI dispatcher thread, writing to a relative path that resolves to the process working directory, with no rotation and no size bound. Logging moves to a dedicated, non-blocking service writing to a bounded, rotating file under the user's local application data directory.
- Journal unhandled exceptions. `App.OnDispatcherUnhandledException` is currently an empty method, so crashes are silent.
- Fix `throw ex;` in `Core/GARMIN/Client.cs` (two occurrences), which destroys the original stack trace.
- Add a `WahooFitToGarmin.Tests` xUnit project wired into the solution.

Explicitly out of scope: no user-visible behaviour changes. The folder watcher, the "keep uploaded activity file" option, toast notifications, the in-app log viewer, theme selection, navigation, settings persistence, and the Garmin upload path all behave exactly as before. The solution must still build and run as a WPF application at the end of this change.

## Capabilities

### New Capabilities

- `runtime-platform`: the supported .NET runtime, target frameworks, language settings, and dependency version policy that every other capability builds on. Defines what runtime the application requires and how package versions are governed.
- `application-logging`: persistent diagnostic logging — where log files live, how they rotate and stay bounded, which severity levels are recorded, that unhandled exceptions are captured, and that logging never blocks the UI thread.

### Modified Capabilities

None. `openspec/specs/` is empty; this is the first change to introduce specs.

## Impact

**Project files**
- `WahooFitToGarmin-Desktop/WahooFitToGarmin-Desktop.csproj` — target framework, SDK, language settings, package references
- `WahooFitToGarmin-Desktop.Core/WahooFitToGarmin-Desktop.Core.csproj` — target framework, language settings, package references
- `WahooFitToGarmin-Desktop.sln` — new test project entry
- New: `Directory.Packages.props`, `WahooFitToGarmin.Tests/`

**Source**
- `WahooFitToGarmin-Desktop/Helpers/LogEntry.cs` — file I/O removed from the constructor
- `WahooFitToGarmin-Desktop/App.xaml.cs` — DI registration for the logger, unhandled exception handler implemented
- `WahooFitToGarmin-Desktop.Core/Services/FileService.cs` — serializer swap with backward-compatible read
- `WahooFitToGarmin-Desktop.Core/GARMIN/Client.cs` — `throw ex;` corrected
- All files importing `Microsoft.Toolkit.Mvvm` — `ViewModels/MainViewModel.cs`, `ViewModels/SettingsViewModel.cs`, `ViewModels/ShellViewModel.cs`, `Services/PageService.cs`

**Dependencies**
- Removed: `Newtonsoft.Json`, `Microsoft.Toolkit.Mvvm`
- Added: `CommunityToolkit.Mvvm` 8.x, a logging library, xUnit
- Unchanged in this change: `Flurl.Http` 4.0.0, `OAuth.DotNetCore`, `MahApps.Metro`, `Microsoft.Toolkit.Uwp.Notifications`, `Hardcodet.NotifyIcon.Wpf`

**Users**
- The .NET 10 Desktop Runtime replaces .NET Core 3.1 as the prerequisite; the README instruction must be updated.
- Existing saved settings continue to load. The log file moves out of the working directory to the local application data folder.

**Downstream changes**
Unblocks `extract-platform-agnostic-core`, and transitively every other change in the modernization sequence.
