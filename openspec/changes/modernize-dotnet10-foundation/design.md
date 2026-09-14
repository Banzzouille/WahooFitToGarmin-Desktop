## Context

The solution is two projects: `WahooFitToGarmin-Desktop` (WPF, `netcoreapp3.1`, `Microsoft.NET.Sdk.WindowsDesktop`) and `WahooFitToGarmin-Desktop.Core` (`netstandard2.0`). Roughly 1 600 lines of C# total, generated from the Windows Template Studio WPF template, plus a hand-written Garmin client under `Core/GARMIN/`.

Three properties of the existing code constrain the approach:

1. **The template plumbing is disposable.** `NavigationService`, `PageService`, `FrameExtensions`, `MenuItemTemplateSelector`, `EnumToBooleanConverter`, and the ten `.xaml` files are all removed in `avalonia-ui-port`. Investing in their quality now is wasted effort.
2. **The view models are rewritten twice downstream** — once in `extract-platform-agnostic-core` when logic moves out of them, once in `avalonia-ui-port`. Any refactor of their internals here will be overwritten.
3. **The Garmin client is deleted in `garmin-di-oauth2-core`.** Only the `throw ex;` fix is worth applying, because a lost stack trace during the auth rewrite would cost debugging time.

This argues for a deliberately conservative change: move the floor, touch nothing that is scheduled for demolition.

## Goals / Non-Goals

**Goals:**
- Both projects build and run on a supported .NET 10 runtime.
- Package versions are declared in one place.
- Diagnostic logging is durable, bounded, and off the UI thread.
- A test project exists and runs, with at least one meaningful test.
- The application's observable behaviour is byte-for-byte identical to today.

**Non-Goals:**
- Cross-platform support. The UI project stays Windows-only here.
- Any MVVM idiom modernisation (`[ObservableProperty]`, `[RelayCommand]`). Namespace swap only.
- Removing `App.Current.Properties`, the `System.Windows.Forms` reference, or the UWP toast stack. All belong to later changes.
- Nullable-clean code. Annotations are enabled; burning down the warnings is progressive.
- Any change to the Garmin authentication or upload flow.

## Decisions

### D1 — Target frameworks: `net10.0` for Core, `net10.0-windows10.0.19041.0` for the UI

The proposal says "retarget both projects to .NET 10", which is imprecise. WPF requires a Windows-flavoured TFM, and the UWP toast stack requires more than that.

`Services/ToastNotificationsService.cs` uses `Windows.UI.Notifications` and `Windows.Data.Xml.Dom` — WinRT projections. Under `netcoreapp3.1` these were supplied transitively by `Microsoft.Toolkit.Uwp.Notifications`. On modern .NET, WinRT projections are only available through a Windows SDK target framework moniker. Therefore:

- Core: `net10.0`
- UI: `net10.0-windows10.0.19041.0`, with `UseWPF` and `UseWindowsForms` retained
- Tests: `net10.0`, referencing Core only

19041 (Windows 10 2004) is chosen as the lowest SDK version that projects the notification APIs while keeping the supported-OS floor low. The Windows-specific TFM disappears in `avalonia-ui-port`.

*Alternative considered:* keep Core on `netstandard2.0` so it stays consumable by other runtimes. Rejected — nothing consumes it, and `netstandard2.0` blocks nullable reference types, `System.Text.Json` source generation, and modern BCL APIs. `Garmin.FIT.Sdk`, needed in `fit-device-emulation`, targets `netstandard2.0` and is consumable from `net10.0` regardless.

*Alternative considered:* move the SDK from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk`. This is required, not optional — `Microsoft.NET.Sdk.WindowsDesktop` is obsolete; `UseWPF`/`UseWindowsForms` on the base SDK is the supported form.

### D2 — MVVM migration is a mechanical namespace swap

`Microsoft.Toolkit.Mvvm` 7.1.2 → `CommunityToolkit.Mvvm` 8.x changes the namespace but keeps `ObservableObject`, `RelayCommand`, and `RelayCommand<T>` API-compatible. Four files import it. The migration is four `using` edits.

The temptation is to adopt the 8.x source generators at the same time. Rejected: it would rewrite every property in three view models that `extract-platform-agnostic-core` then rewrites again, turning a reviewable diff into an unreviewable one. Source generators get adopted when the view models are rewritten for real.

### D3 — `ILogger<T>` as the abstraction, a rolling-file provider as the sink

`Microsoft.Extensions.Hosting` already brings `Microsoft.Extensions.Logging` into the composition root, and Core must not depend on a specific logging implementation — it is consumed by an Avalonia host later. So: `ILogger<T>` everywhere in code, the concrete sink configured once in `App.ConfigureServices`.

Serilog with `WriteTo.File(rollingInterval: Day, retainedFileCountLimit, fileSizeLimitBytes, rollOnFileSizeLimit)` is the sink, via `Serilog.Extensions.Hosting`. It gives bounded rotation out of the box, which is the actual requirement.

*Alternative considered:* the built-in `Microsoft.Extensions.Logging` file provider. There isn't one — file logging requires a third-party provider regardless. *Alternative considered:* NLog. Equivalent capability; Serilog is chosen for ubiquity in .NET generic-host applications.

Core code calls `ILogger<T>` only. Serilog is referenced exclusively by the UI project.

### D4 — The in-app log viewer becomes a logging provider, not a parallel path

Today there are two independent logging paths: `MainViewModel.Log()` appends a `LogEntry` to an `ObservableCollection` for the UI, and the `LogEntry` constructor separately appends a line to a file. They will drift — anything logged from Core appears in neither.

Instead, one path: a custom `ILoggerProvider` whose logger marshals formatted entries onto the UI dispatcher and appends them to the observable collection. `MainPage.xaml` binds to `LogDateTime`/`LogMessage`, so `LogEntry` is kept as a plain DTO with its file I/O removed; the `DataTemplate` needs no edit.

Consequence: log lines originating in Core (upload failures, auth errors) become visible in the app for the first time. This is a behaviour improvement, not a regression, and is compatible with the "no user-visible change" constraint in the sense that nothing is lost.

The provider bounds the collection (cap on retained entries, oldest trimmed) so a long-running session cannot grow it without limit.

### D5 — Log location follows the existing settings location

`PersistAndRestoreService` already resolves `Environment.SpecialFolder.LocalApplicationData` + `AppConfig.ConfigurationsFolder`. Logs go to a sibling `Logs` folder under the same root.

One detail is fixed in passing: `appsettings.json` declares `"configurationsFolder": "WahooFitToGarmin_Desktop\\Configurations"` with a hard-coded backslash. That value is later joined by `Path.Combine`, so on macOS it would produce a single file named `WahooFitToGarmin_Desktop\Configurations`. The separator is normalised now, while the cost is zero.

### D6 — `System.Text.Json` swap keeps the base64 envelope

`FileService.Save` serialises, then base64-encodes; `FileService.Read` sniffs for a leading `{` and falls back to base64-decoding. The base64 layer is obfuscation, not encryption — the README says so explicitly.

It would be tempting to drop it and write readable JSON. Rejected for sequencing reasons: the file currently contains a Garmin password in clear text, and making that more legible before `garmin-di-oauth2-core` removes credential storage entirely is the wrong order of operations. The envelope stays; only the serializer changes. Readable JSON lands in `garmin-di-oauth2-core`, once there is no password left in the file.

The existing `{`-sniffing read path is retained, so files written by either serializer load.

### D7 — Property bag values are normalised to strings, in both directions

This decision was written on a false premise and is corrected here.

The original claim was that `System.Text.Json` has no support for non-generic
dictionaries, and that handing it `App.Current.Properties` would throw or emit
nothing useful. That is not true on .NET 10: serialization writes the bag
correctly, and deserialization into `IDictionary` succeeds, yielding a
dictionary whose values are `JsonElement`. The test written to prove the
incompatibility failed, which is how the error surfaced.

The conversion is kept anyway, on narrower grounds. Reading straight into the
bag leaves `JsonElement` values sitting beside the plain strings and booleans
the settings page writes at runtime, so the bag's contents depend on whether a
value came from disk or from the user in this session. Every consumer calls
`ToString` or `bool.TryParse`, so normalising to strings on the way in and on
the way out costs nothing and removes that distinction.

`PropertyBagSerialization` therefore projects the bag to
`Dictionary<string, string>` on save, and flattens each `JsonElement` to its
string form on restore. The flattening is not cosmetic: version 1.1.0 stored
the keep-uploaded-file option as an unquoted JSON boolean, so a file written by
that version cannot be read into a string dictionary directly.

It lives in the core library rather than beside its caller because it is pure,
it carries the backward-compatibility rule, and a platform-neutral home is the
only one the tests can reach.

### D8 — Nullable enabled, warnings not errors

`<Nullable>enable</Nullable>` on a codebase written without it produces a large warning count. Treating them as errors would force annotating code scheduled for deletion.

Policy: enabled everywhere, `TreatWarningsAsErrors` off. Core is annotated properly in this change — it survives all downstream changes. UI warnings are burned down as files are rewritten in later changes. `Directory.Build.props` carries the shared settings so the policy is declared once.

### D9 — `ApplicationInfoService` stops reading the assembly file path

It resolves the version through `Assembly.GetExecutingAssembly().Location` + `FileVersionInfo`. `Location` returns an empty string under single-file publish, which `cross-platform-packaging-ci` will use, so this throws at that point. It is a three-line fix to read `AssemblyInformationalVersionAttribute` instead; doing it now avoids a confusing failure in a change that has nothing to do with versioning.

### D10 — Test scope and framework

The test project uses MSTest with Moq, and runs on Microsoft.Testing.Platform: the .NET 10 SDK no longer supports VSTest from `dotnet test`, so the repository opts into the new runner through `global.json`. No test SDK package and no VSTest adapter are needed.

The test project references Core only; the UI project's Windows TFM would make the tests unrunnable on the macOS CI leg introduced later.

In scope for this change: `FileService` round-trip, legacy base64 read compatibility, legacy plain-JSON read compatibility, `Dictionary<string, string>` projection fidelity, and a smoke test asserting the logger writes to the configured directory and rotates. No network tests against Garmin.

## Addendum — D14: the solution moves to the `.slnx` format

The legacy solution format is a line-oriented dialect that only Visual Studio ever
wrote comfortably: forty lines of project identifiers, configuration mappings and
globals, which merge badly and which nobody edits by hand without regret.

The SDK now converts it in one command, and the result for this repository is ten
lines of XML naming three projects. Merge conflicts in a solution file become
readable, and adding a project is a one-line diff.

The cost is tooling age: the format is newer than some of the editors and scripts
that might open it. This project builds and tests entirely through the SDK, which
supports it, and the packaging change later consumes it the same way. The one
thing worth confirming rather than assuming is that whatever editor a contributor
uses can open it — recorded as a task rather than hoped for.

## Risks / Trade-offs

**`MahApps.Metro` 2.4.9 may not build against `net10.0-windows`** → Bump to the current 2.x release as part of this change. MahApps is deleted in `avalonia-ui-port`, so any version that compiles is acceptable; no time is spent on theming regressions beyond confirming the three themes still apply.

**`Microsoft.Toolkit.Uwp.Notifications` 7.1.2 is deprecated and may not resolve on a Windows SDK TFM** → Its successor is `CommunityToolkit.WinUI.Notifications`. If 7.1.2 fails to build, switch to the successor package; the toast code is ~40 lines and is deleted in `avalonia-ui-port` regardless. Falling back to a no-op notifier is acceptable if both fail, since a broken build blocks everything downstream while a missing toast does not.

**`Flurl.Http` 4.0.0 and `OAuth.DotNetCore` 3.0.1 on .NET 10 are unverified** → Verify in the first hour of implementation, before any other work. Flurl 4 targets `netstandard2.0` and should load; `OAuth.DotNetCore` is a small signing helper and is deleted in `garmin-di-oauth2-core` anyway. If either fails, the fallback is inlining the OAuth1 signing, but this is unlikely to be needed.

**`Resources.Designer.cs` may need regeneration** under the new SDK → It is generated by `PublicResXFileCodeGenerator` and checked in. If the build complains, delete and regenerate. Low impact, but it surprises people.

**Enabling nullable surfaces a warning count that feels alarming** → Communicated as expected in D8. Reviewers should look at Core warnings only; UI warnings are tracked, not fixed.

**Logging Core events into the in-app viewer (D4) changes what users see** → Strictly additive. Verified manually: the existing messages still appear, in the same order, with the same wording.

**The "no behaviour change" claim is only as good as the verification** → There is no test suite to prove it. Mitigation is a manual smoke checklist run before and after: settings load, folder selection, theme switch, a real `.fit` dropped into the watched folder, `KeepUploadedActivityFile` both on and off, toast on arrival.

## Migration Plan

Branch `chore/net10-foundation`, merged as one unit.

1. Verify `Flurl.Http` and `OAuth.DotNetCore` resolve and run on .NET 10 — a throwaway console spike. If this fails, stop and re-plan.
2. `Directory.Build.props` and `Directory.Packages.props`.
3. Retarget Core, annotate it for nullable, add the test project.
4. Retarget the UI project, swap the SDK, bump MahApps and the notifications package.
5. MVVM namespace swap.
6. Logging: Serilog wiring, the observable-collection provider, `LogEntry` reduced to a DTO, unhandled exception handler.
7. `FileService` serializer swap plus the `IDictionary` projection, with tests.
8. `throw ex;` and `ApplicationInfoService` fixes.
9. Manual smoke checklist.

No data migration. Existing settings files load unchanged.

**Rollback:** revert the branch. The only on-disk change is the log file's new location — the old `WahooFitToGarmin-Desktop.log` in the working directory is left in place, not deleted, so a rollback finds it intact.

## Open Questions

Resolved during the compatibility spike, ahead of implementation:

- **Which `MahApps.Metro` version builds cleanly against `net10.0-windows`?** The version already referenced, 2.4.9, builds without complaint. No bump is needed, which removes the theming-regression risk entirely.
- **Does `Microsoft.Toolkit.Uwp.Notifications` 7.1.2 resolve on the Windows SDK target framework?** Yes. `CommunityToolkit.WinUI.Notifications` is not required, and the toast code needs no import changes.
- **Is `net10.0-windows10.0.19041.0` sufficient for the WinRT notification projections?** Yes. `Windows.UI.Notifications` and `Windows.Data.Xml.Dom` both resolve at that version, so the supported-OS floor stays where D1 put it.
- **Do `Flurl.Http` 4.0.0 and `OAuth.DotNetCore` 3.0.1 run on .NET 10?** Yes. Flurl performed a real HTTPS request and the OAuth1 helper produced a signed authorization header, both on .NET 10.0.11.

Still open:

- Should the retained-log-count and size-cap values be configurable in `appsettings.json` or fixed constants? Leaning fixed, since no one will tune them; the `application-logging` spec fixes them as constants.

## Addendum — D13: Windows targets are built from macOS with `EnableWindowsTargeting`

The spike established something the original design assumed impossible: a project targeting `net10.0-windows10.0.19041.0` with WPF and Windows Forms enabled **compiles on macOS**, provided `EnableWindowsTargeting` is set. MahApps, the UWP notification package, and the WinRT projections all resolve there too.

This property is therefore set in `Directory.Build.props`, so that the whole solution builds on either operating system. It is harmless on Windows and costs nothing.

What it does **not** give is the ability to run the application: WPF executes only on Windows. So compilation, package resolution, and the automated tests are all available here, while anything requiring the application to start — the behaviour baseline and the smoke checklist — still needs a Windows machine.

The practical consequence is that the change splits cleanly: everything except the runtime verification can be done and reviewed from macOS.
