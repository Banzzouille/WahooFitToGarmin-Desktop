## Why

The application's entire operating logic lives in a WPF view model: `MainViewModel` constructs the file watcher, reads settings out of `App.Current.Properties`, authenticates against Garmin, uploads, and deletes files. None of it can be tested, and none of it can be reused by a non-WPF user interface — so the Avalonia port would have to reimplement it rather than port it. The same view model also carries four defects that cost users uploads today, and those defects are cheapest to fix while the code is being moved.

## What Changes

- Move file watching, upload orchestration, and settings access out of the view models into the core library, behind abstractions the user interface implements: watcher, upload pipeline, settings store, notifier, folder picker, and user interface dispatcher.
- Remove `App.Current.Properties` as the application's settings store. Settings become a typed object with a dedicated store. **BREAKING** at the storage layer: the existing file is read and migrated on first launch.
- Settings take effect without restarting. Today `MainViewModel` reads configuration once in its constructor, so changing the watched folder or the keep-uploaded-file option requires a restart, and the application says so in its own log.
- **Fix truncated uploads.** `FileSystemWatcher.Created` fires when a file is created, not when writing finishes. Dropbox writes progressively, so the current code can upload a partial `.fit` file. The pipeline waits for the file to stabilise before reading it.
- **Fix files missed while the application was closed.** There is no initial scan today; `.fit` files already present when the application starts are ignored forever. The pipeline scans the watched folder at startup. **BREAKING**: to avoid mass-uploading a folder's entire history on first launch after this change, files already present at that moment are recorded as seen rather than uploaded.
- Add an idempotence record so a file is never uploaded twice, whether it is seen by the initial scan, by the watcher, or by both.
- **Fix expired sessions.** `Client.IsOAuthValid` exists but is never called; the current check is only whether a token object exists. A session that has expired during a long-running instance produces a failed upload. The pipeline validates and refreshes the session before uploading.
- **Fix silently lost failures.** `MainViewModel` starts the upload without awaiting it, and the authentication call sits outside the surrounding try block, so an authentication failure disappears with no log entry and no notification. Every failure is logged and surfaced.
- Add retry with exponential backoff for transient upload failures, with a bounded attempt count.
- Report an upload rejected as a duplicate as a distinct outcome rather than treating it as an unexplained success.
- Expose counters for processed, failed, and duplicate activities, replacing the `{Binding Count}` placeholder on the main page that binds to a property that does not exist.
- Keep the WPF user interface working throughout. Platform implementations of the new abstractions are supplied for WPF; they are replaced, not written for the first time, by `avalonia-ui-port`.

Deliberately not in this change: encrypted credential storage. `garmin-di-oauth2-core` removes stored credentials entirely, so introducing a secret store here would be building something to delete one change later.

## Capabilities

### New Capabilities

- `activity-watch-pipeline`: how activity files are discovered — watching, startup scan, waiting for writes to complete, avoiding duplicate processing, and what happens to the source file after a successful upload.
- `upload-resilience`: how uploads survive transient failure and expired sessions — session validation, retry policy, duplicate handling, failure reporting, and the processed, failed, and duplicate counters.
- `application-settings`: what the application stores, where, in what shape, how settings written by earlier versions are migrated, and the requirement that changes take effect without a restart.

### Modified Capabilities

None. `runtime-platform` and `application-logging` keep their requirements; this change adds behaviour rather than altering what those capabilities already guarantee.

## Impact

**Core library — new**
- Watcher service, upload pipeline, settings store, and the abstractions for notification, folder selection, and user interface dispatch
- Idempotence record of processed files

**Core library — modified**
- `GARMIN/Client.cs` — session validity is honoured before upload
- `Services/FileService.cs` — settings shape changes
- `Contracts/Services/IFileService.cs`

**Desktop project — modified**
- `ViewModels/MainViewModel.cs` — reduced to presentation: log display and counters
- `ViewModels/SettingsViewModel.cs` — bound to the settings store instead of `App.Current.Properties`
- `Services/PersistAndRestoreService.cs` — replaced by the settings store
- `Models/AppConfig.cs` — split between genuine application configuration and user settings
- `App.xaml.cs` — registration of the new services
- WPF implementations of the notifier, folder picker, and dispatcher abstractions

**Users**
- Files arriving while Dropbox is still writing are uploaded complete rather than truncated.
- Settings changes apply immediately.
- On first launch after this change, files already sitting in the watched folder are marked as seen and not uploaded.
- Failures that previously vanished now appear in the log and in the failure counter.

**Downstream changes**
Required by `avalonia-ui-port`, which ports view models that must already be free of application logic, and by `fit-device-emulation`, which inserts a file transformation step into the upload pipeline this change creates.
