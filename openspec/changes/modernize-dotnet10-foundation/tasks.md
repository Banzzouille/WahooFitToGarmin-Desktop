## 1. Compatibility spike (gate)

- [x] 1.1 Create branch `chore/net10-foundation`
- [x] 1.2 Record the current behaviour baseline in `baseline-log-messages.md`, covering startup, file detection, Garmin connection, and upload. Captured by static extraction from `MainViewModel` at commit `676f35a` rather than by launching the application, because WPF runs only on Windows and this change was implemented on macOS. Message wording is exact; runtime ordering is inferred from the call sites
- [ ] 1.2a On a Windows machine, run version 1.1.0 through a full cycle and confirm the emitted order matches the ordering stated in `baseline-log-messages.md`; correct the file if it does not. Required before merge, since tasks 6.11 and 11.8 compare against it
- [x] 1.3 In a throwaway `net10.0` console project, verify `Flurl.Http` 4.0.0 loads and performs a real HTTPS GET
- [x] 1.4 In the same spike, verify `OAuth.DotNetCore` 3.0.1 loads and produces an OAuth1 authorization header
- [x] 1.5 Verify `MahApps.Metro` resolves against a `net10.0-windows10.0.19041.0` project; record the working version
- [x] 1.6 Verify `Microsoft.Toolkit.Uwp.Notifications` 7.1.2 resolves on the same target; if it does not, confirm `CommunityToolkit.WinUI.Notifications` as the replacement and record which is used
- [x] 1.7 Confirm `Windows.UI.Notifications` and `Windows.Data.Xml.Dom` projections resolve at `net10.0-windows10.0.19041.0`; raise the SDK version only if they do not
- [x] 1.8 STOP AND RE-PLAN if 1.3 or 1.4 fails — the Garmin client cannot run on .NET 10 without a replacement
- [x] 1.9 Delete the spike project

## 2. Build infrastructure

- [x] 2.1 Add `Directory.Build.props` at the repository root declaring `Nullable`, `ImplicitUsings`, and `LangVersion`
- [x] 2.2 Confirm `TreatWarningsAsErrors` is not enabled
- [x] 2.3 Add `Directory.Packages.props` with `ManagePackageVersionsCentrally` enabled and a `PackageVersion` entry for every package currently referenced, using the versions confirmed in group 1
- [x] 2.4 Strip the `Version` attribute from every `PackageReference` in both project files
- [x] 2.5 Verify the solution restores with centrally managed versions before changing any target framework

## 2b. Solution format

- [x] 2b.1 Migrate the solution from the legacy `.sln` format to `.slnx` with `dotnet sln migrate`
- [x] 2b.2 Verify the whole solution builds from the `.slnx`
- [x] 2b.3 Verify `dotnet test` runs from the `.slnx`
- [x] 2b.4 Delete the legacy `.sln`
- [ ] 2b.5 Confirm the `.slnx` opens in the editors the project's contributors use, since tooling support for the format is newer than the format itself

## 3. Core library retarget

- [x] 3.1 Change `WahooFitToGarmin-Desktop.Core.csproj` target framework from `netstandard2.0` to `net10.0`
- [x] 3.2 Remove `Nullable`, `LangVersion`, and `ImplicitUsings` from the project file now that `Directory.Build.props` supplies them
- [x] 3.3 Build Core and resolve compilation errors introduced by the retarget
- [x] 3.4 Annotate Core for nullable reference types until it compiles without nullable warnings
- [x] 3.5 Confirm Core references no WPF, Windows Forms, or Windows SDK projection assembly

## 4. Test project

- [x] 4.1 Create `WahooFitToGarmin.Tests` targeting `net10.0` with MSTest and Moq
- [x] 4.2 Reference the Core project only; do not reference the desktop project
- [x] 4.3 Add the test project to `WahooFitToGarmin-Desktop.sln`
- [x] 4.4 Add a placeholder test and confirm `dotnet test` discovers and runs it
- [x] 4.5 Opt the repository into the Microsoft.Testing.Platform runner through `global.json`, since the .NET 10 SDK no longer supports VSTest from `dotnet test`

## 5. Desktop project retarget

- [x] 5.1 Change the project SDK from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk`
- [x] 5.2 Set the target framework to `net10.0-windows10.0.19041.0`, keeping `UseWPF` and `UseWindowsForms` enabled
- [x] 5.3 Upgrade `Microsoft.Extensions.Hosting` to 10.x (10.0.12, declared centrally)
- [x] 5.4 Upgrade `MahApps.Metro` to the version confirmed in 1.5 — no upgrade needed, the referenced 2.4.9 builds against the new target framework unchanged
- [x] 5.5 Apply the notifications package decision from 1.6 — 7.1.2 resolves on the Windows SDK target, so the package stands and no `using` directive changed, adjusting `using` directives in `ToastNotificationsService.cs`, `ToastNotificationsService.Samples.cs`, `IToastNotificationsService.cs`, and `App.xaml.cs` if the package changed
- [x] 5.6 Build the desktop project and resolve compilation errors — one ambiguity between the Windows Forms and WPF `Application` types, caused by implicit usings importing `System.Windows.Forms` globally; the implicit import is removed in the project file and the single consumer keeps its explicit one
- [x] 5.7 Regenerate `Properties/Resources.Designer.cs` if the build reports a generator mismatch — no mismatch reported, left untouched
- [ ] 5.8 Launch the application and confirm the shell window, navigation, main page, and settings page render

## 6. MVVM package migration

- [x] 6.1 Replace the `Microsoft.Toolkit.Mvvm` package reference with `CommunityToolkit.Mvvm` 8.x
- [x] 6.2 Update the `using` directives in `ViewModels/MainViewModel.cs`, `ViewModels/SettingsViewModel.cs`, `ViewModels/ShellViewModel.cs`, and `Services/PageService.cs`
- [x] 6.3 Build and confirm no other file referenced the old namespace
- [ ] 6.4 Launch the application and confirm property change notification and all commands still work — theme radio buttons, folder selection, GitHub link, navigation, back button
- [x] 6.5 Confirm no XAML binding path was modified

## 7. Logging pipeline

- [ ] 7.1 Add Serilog with the hosting integration to the desktop project only; Core must not reference it
- [ ] 7.2 Configure the rolling file sink: daily interval, 10 MB per-file limit with roll on size, 7 retained files, written under the local application data `Logs` folder
- [ ] 7.3 Create the `Logs` directory at startup if it is absent
- [ ] 7.4 Configure the output template so each line carries its severity level
- [ ] 7.5 Implement an `ILoggerProvider` that marshals formatted entries onto the UI dispatcher and appends them to the observable collection backing the log viewer
- [ ] 7.6 Bound that collection, trimming oldest entries past the cap
- [ ] 7.7 Reduce `Helpers/LogEntry.cs` to a plain DTO; remove the `File.AppendText` call from its constructor
- [ ] 7.8 Confirm `MainPage.xaml` requires no edit, since `LogDateTime` and `LogMessage` are unchanged
- [ ] 7.9 Replace `MainViewModel.Log()` calls with `ILogger<MainViewModel>` calls, preserving the exact message wording captured in 1.2
- [ ] 7.10 Inject `ILogger<T>` into Core's Garmin client and log upload and authentication failures
- [ ] 7.11 Implement `App.OnDispatcherUnhandledException` to log exception type, message, and stack trace
- [ ] 7.12 Verify the file sink failing does not crash the app, by pointing it at a read-only path once

## 8. Settings serialization

- [ ] 8.1 Replace `Newtonsoft.Json` with `System.Text.Json` in `Core/Services/FileService.cs`
- [ ] 8.2 Keep the base64 envelope on write and keep the leading-brace sniffing on read
- [ ] 8.3 Project `App.Current.Properties` to `Dictionary<string, string>` before saving in `PersistAndRestoreService`, and rehydrate on restore
- [ ] 8.4 Remove the `Newtonsoft.Json` package reference from Core
- [ ] 8.5 Normalise the backslash in `appsettings.json` `configurationsFolder` to a platform-neutral separator
- [ ] 8.6 Test: round-trip of all five stored keys returns identical values
- [ ] 8.7 Test: a base64-wrapped settings file produced by version 1.1.0 is restored correctly
- [ ] 8.8 Test: a plain JSON settings file is restored correctly
- [ ] 8.9 Test: non-generic dictionary content is not lost through serialization
- [ ] 8.10 Manual check: launch with a settings file from the previous version and confirm every setting is still populated

## 9. Diagnostics fixes

- [ ] 9.1 Replace both `throw ex;` occurrences in `Core/GARMIN/Client.cs` with `throw;`
- [ ] 9.2 Search the solution for any other stack-trace-resetting rethrow
- [ ] 9.3 Rewrite `ApplicationInfoService.GetVersion()` to read `AssemblyInformationalVersionAttribute` instead of `Assembly.Location` and `FileVersionInfo`
- [ ] 9.4 Confirm the settings page still displays a non-empty version

## 10. Documentation

- [ ] 10.1 Update the README prerequisite from .NET Core 3.1 Desktop Runtime to .NET 10 Desktop Runtime, replacing the download link
- [ ] 10.2 Document the new log file location in the README
- [ ] 10.3 Note that the previous `WahooFitToGarmin-Desktop.log` in the working directory is no longer written and can be deleted

## 11. Verification

- [ ] 11.1 `dotnet build` succeeds for the whole solution
- [ ] 11.2 `dotnet test` passes
- [ ] 11.3 Smoke: settings load from an existing installation
- [ ] 11.4 Smoke: folder selection dialog opens and persists the chosen path
- [ ] 11.5 Smoke: all three themes apply
- [ ] 11.6 Smoke: drop a real `.fit` file into the watched folder and confirm detection, notification, and upload attempt
- [ ] 11.7 Smoke: confirm the file is deleted when keep-uploaded-file is off, and retained when on
- [ ] 11.8 Smoke: compare emitted log messages against the 1.2 baseline — same messages, same order
- [ ] 11.9 Confirm a log file exists under local application data and none in the working directory
- [ ] 11.10 Confirm an error logged from Core appears in the in-app log viewer
- [ ] 11.11 Review the nullable warning count; Core must be clean, desktop warnings are recorded as follow-up work
