# Behaviour baseline — log messages before the migration

Reference for tasks 6.11 and 11.8, which require the migrated application to emit the same operational messages, in the same order, as version 1.1.0.

**How this was captured.** By static extraction from `ViewModels/MainViewModel.cs` at commit `676f35a`, not by running the application. WPF executes only on Windows and this change was implemented on macOS. Every literal below is exact. What static extraction cannot establish is the runtime order and which branches a real session takes — task 1.2a covers that on a Windows machine before merge.

**Format.** Each entry is rendered by `LogEntry` as `{timestamp:u}  : {message}` in the file sink, and as two bound columns in the in-app viewer. Placeholders in braces are interpolated values.

## Startup, in construction order

| # | Condition | Message |
|---|---|---|
| 1 | always, first entry in the collection | `Starting .......` |
| 2 | watched folder empty | `Please select folder to watch for in settings` |
| 3 | always | `Wahoo folder to watch for : {folder}` |
| 4 | login or password empty | `Please enter your Garmin login and password in settings` |
| 5 | folder missing, or credentials incomplete | `Please feel correctly yours app settings in settings screen and restart the application to apply them` |
| 6 | always, last startup entry | `Starting uploader ......` |

Entry 1 is emitted by the collection initialiser, before `DumpSettings`. Entries 2 to 4 come from `DumpSettings`. Entry 5 is emitted only when the watcher is not started. Entry 6 is always last.

The wording of entries 2, 4, and 5 is retained verbatim for comparison purposes only. Entry 5 instructs the user to restart; `extract-platform-agnostic-core` removes that instruction, and its own specification forbids emitting it.

## File detected

| # | Message |
|---|---|
| 7 | `A new file is coming => {fileName}` |
| 8 | `-------------------------------------------------------------------------------` |

Entry 7 is emitted before the upload starts. Entry 8 is emitted immediately afterwards, on the detection thread — so in a real session it appears **before** the upload messages below, not after them. A toast notification titled `A new file is coming` with the file name as body accompanies entry 7.

## Upload

| # | Condition | Message |
|---|---|---|
| 9 | no session yet | `Connection to Garmin Connect server` |
| 10 | authentication succeeded | `Connection success.` |
| 11 | session already present | `Already logged.` |
| 12 | always | `Uploading file {fullPath}` |
| 13 | upload accepted | `Activity uploaded {fullPath}` |
| 14 | upload accepted | `Activity uploaded :{serviceMessage}` |
| 15 | service reported a failure | `Failed to upload activity to Garmin : {serviceMessage}` |
| 16 | exception during upload | `Failed to upload activity {fullPath} : {exceptionMessage}` |

Entries 9 to 11 are mutually exclusive. Entries 13 and 14 are emitted as a pair. Entry 12 uses the full path, not the file name — unlike entry 7.

Note that authentication failures produce **no** message at all: the authentication call sits outside the try block that catches upload failures, so an authentication exception escapes an unawaited task and is lost. Absence of output on that path is part of the current behaviour, and is a defect `extract-platform-agnostic-core` fixes rather than behaviour to preserve.

## Not emitted anywhere

The core library produces no log output at all in version 1.1.0. Anything appearing from `WahooFitToGarmin_Desktop.Core` after this change is new, and is additive rather than a regression.
