## Context

Everything the application actually does happens in one constructor. `MainViewModel` builds a `FileSystemWatcher`, reads five values out of `App.Current.Properties`, and wires a handler that authenticates, uploads, and deletes. The handler is not awaited. The authentication call sits outside the try block that catches upload failures.

That structure produces the four defects listed in the proposal, and it is also why none of this can be tested: there is no seam between the file system, the Garmin client, and WPF's static application object.

Three constraints shape the design:

1. **The WPF user interface must keep working.** This change ships before `avalonia-ui-port`, so every abstraction introduced here needs a WPF implementation that is thrown away one change later. That is acceptable — the alternative is a change that cannot be merged on its own.
2. **`fit-device-emulation` inserts a transformation step** between reading the file and uploading it. The pipeline has to have an obvious place to put it.
3. **`garmin-di-oauth2-core` replaces authentication entirely.** The pipeline must depend on an abstraction of "a usable Garmin session", not on today's `IClient`, or it will be rewritten twice.

## Goals / Non-Goals

**Goals:**
- No application logic in a view model.
- The pipeline is testable without a user interface, a real file system watcher, or a network.
- A `.fit` file is uploaded exactly once, complete, or the failure is visible.
- Settings changes apply without a restart.

**Non-Goals:**
- Encrypted secret storage. `garmin-di-oauth2-core` removes stored credentials.
- Parallel uploads. Serial processing is sufficient for the volume involved and keeps the log readable.
- Watching more than one folder, or recursive watching. Neither exists today.
- A user-visible queue or history view. The log and the counters are the interface.
- Changing the Garmin client's authentication flow, which is the next change's job.

## Decisions

### D1 — The pipeline is a hosted service in the core library

The pipeline runs for the application's lifetime, so it is expressed as an `IHostedService`, started and stopped by the generic host that already exists in `App.xaml.cs`. The core library references `Microsoft.Extensions.Hosting.Abstractions` — platform-neutral, and already in the dependency graph — rather than the full hosting package.

This keeps the user interface's job to exactly three things: render state, forward user intent, and supply platform implementations.

### D2 — File readiness is decided by stability polling, not by a lock probe

`FileSystemWatcher.Created` fires when the file appears, and Dropbox continues writing afterwards. Something must decide when the file is complete.

The obvious approach — try to open the file with an exclusive share mode, and treat success as "the writer is done" — is unreliable here. On Unix, .NET's file locking is advisory, so an exclusive open can succeed while another process is still writing. Since macOS is a target platform two changes later, an approach that only works on Windows is a trap.

The pipeline therefore polls length and last-write time, and considers a file ready when both are unchanged across consecutive checks separated by a quiet interval. A successful read-only open is used as an additional confirmation rather than as the primary signal. A file that never stabilises within a bounded timeout is logged as failed rather than retried forever.

Dropbox clients also write to a temporary name and rename into place, in which case the file is complete the moment the watcher sees it. Both patterns end at the same place — a file whose size stops changing — so one mechanism covers both.

### D3 — Idempotence is keyed on content, not on path

The record of what has already been processed has to survive Dropbox re-downloading a file, the user moving the folder, and the application restarting. A path is not stable under any of those. A file name alone collides.

The key is a hash of the file's content. The files are small, so hashing costs nothing meaningful, and it makes the guarantee exact: the same activity is never uploaded twice, however it arrives.

The record is a JSON file alongside the settings, holding the hash, the original file name, the outcome, and a timestamp. It is pruned so it cannot grow without bound.

Garmin's own duplicate detection is the backstop if the record is ever lost — which is precisely why duplicate responses are treated as a first-class outcome in D6 rather than as an error.

### D4 — The first run establishes a baseline instead of uploading history

Adding a startup scan to an application that never had one is dangerous: a user with a year of `.fit` files in their Dropbox folder would see all of them pushed to Garmin Connect on first launch, and cleaning that up on Garmin's side is tedious.

On the first run after this change — detected by the absence of the processed-file record — every file already present is written into the record as seen, with no upload. From then on, the startup scan does what it is meant to do: catch files that arrived while the application was closed.

The count of baselined files is logged explicitly, so the behaviour is visible rather than mysterious.

To avoid a race, the watcher starts before the baseline scan runs. A file that arrives during the scan is deduplicated by the record rather than lost or double-handled.

*Alternative considered:* prompt the user to choose. Rejected for this change — it requires user interface work in a change that is deliberately user-interface-free, and the safe default is the one that cannot cause damage.

### D5 — Retry distinguishes transient from permanent, and is hand-rolled

Failure handling is decided by response class:

| Condition | Behaviour |
|---|---|
| Network error, timeout, 5xx | Retry with exponential backoff, bounded attempts |
| 401 | Refresh the session once, then retry once |
| 409 | Duplicate — a terminal outcome, not a failure, no retry |
| Other 4xx | Permanent — logged as failed, no retry |

Retrying a permanent failure wastes time and floods the log; not retrying a transient one loses an upload. The distinction is the whole point.

The retry loop is written by hand rather than taking a policy library. Three attempts with backoff and jitter is roughly thirty lines and is directly testable with a fake client. Taking a dependency to avoid thirty lines in a project whose current problem is aged dependencies would be poor judgement.

### D6 — The source file is deleted only on confirmed upload, never on duplicate

Today the file is deleted when the response carries an upload identifier, and kept when Garmin reports a duplicate. That conservative behaviour is preserved deliberately.

A duplicate response means Garmin already has the activity, so deleting would probably be safe — but "probably" is not good enough for irreversibly deleting a user's data. The duplicate is logged and counted, and the file stays. The user can delete it themselves.

### D7 — Application configuration and user settings are separated

`AppConfig` currently mixes two unrelated things: values that ship with the application and never change at runtime — the configuration folder name, the properties file name, the repository URL — and values the user edits, which are also shadowed in `App.Current.Properties`. Two sources of truth for the same data, kept in sync by hand.

They split. Application configuration stays in `appsettings.json`, read-only. User settings become a typed object owned by a settings store, which is the single source of truth, persists on change, and raises a change notification.

Live reload is in-process: the pipeline subscribes to the store's change notification. It does not watch the settings file. When the watched folder changes, the pipeline stops the old watcher and starts a new one; when the keep-uploaded-file option changes, the next upload observes the new value.

Migration reads the existing base64-wrapped file, maps its five keys onto the typed object, writes the new file, and leaves the old file in place with a backup suffix.

### D8 — Processing is serialised through a queue

Discovery and processing are separated by an in-memory queue, consumed by a single worker. The watcher callback does nothing but enqueue, so it never blocks and never performs input or output.

Serial processing means a burst of files uploads one at a time. That is a deliberate choice: it keeps log output in a comprehensible order, avoids several simultaneous authentication attempts against a service already known to rate-limit, and removes a class of concurrency bug from a codebase that currently has no tests at all.

### D9 — The Garmin session is an abstraction from the start

The pipeline depends on an interface expressing "give me a valid session, refreshing if needed", implemented today over the existing `IClient` and its unused `IsOAuthValid` property. `garmin-di-oauth2-core` replaces the implementation with the DI token flow without touching the pipeline.

This is also where the expired-session defect is fixed: validity is asked of the session abstraction before every upload, instead of checking whether a token object happens to be non-null.

### D10 — Counters are session-scoped pipeline state

Processed, failed, and duplicate counts live on the pipeline's observable state and reset when the application restarts. They describe the current session, which is what the main page shows. Persisting them would imply a history feature that does not exist.

### D11 — The transformation seam for device emulation is created now

The pipeline reads the file, then passes the bytes through an ordered set of transformations, then uploads. This change ships with that set empty. `fit-device-emulation` adds one member to it and needs no other structural change.

Defining the seam now costs one interface and prevents the next change from restructuring the pipeline.

## Risks / Trade-offs

**Stability polling adds latency between a file appearing and its upload** → Bounded by the quiet interval, which is measured in seconds. Uploading seconds later is strictly better than uploading a truncated file, which is the current behaviour.

**A file could stabilise momentarily mid-write** — a slow network pause during a Dropbox download could look like a completed write → Requiring stability across consecutive checks rather than a single observation, combined with a successful read-only open, makes this unlikely. The residual case produces a corrupt upload that Garmin rejects, which is logged as a failure and visible, rather than silent.

**The baseline decision means a new user who deliberately placed files in the folder sees nothing uploaded** → The count is logged explicitly so the behaviour is discoverable rather than silent. Whether to offer an explicit "import files already present" action is left as an open question rather than guessed at.

**Content hashing reads every file twice** — once to hash, once to upload → The file is read once into memory and both operations use those bytes. Activity files are small enough that this is not a concern.

**The processed-file record is another file that can be corrupted or deleted** → A missing or unreadable record degrades to Garmin's duplicate detection: files get re-offered, Garmin answers 409, and the pipeline records them as duplicates without deleting anything. The failure mode is noise, not data loss.

**WPF implementations of the new abstractions are written to be deleted one change later** → Roughly fifty lines of adapter over code that already exists. The alternative is an unmergeable change.

**`FileSystemWatcher` behaviour on macOS is untested at this point** → This change is developed and verified on Windows. `avalonia-ui-port` carries the task that validates real Dropbox writes on macOS, with polling as the contingency. The queue and stability logic are deliberately independent of the watcher, so a polling discovery source can be substituted without touching the rest of the pipeline.

## Migration Plan

Branch `refactor/platform-agnostic-core`.

1. Define the abstractions in the core library: discovery source, settings store, session, transformation step, notifier, folder picker, dispatcher.
2. Implement the settings store with migration from the existing file, covered by tests.
3. Implement the processed-file record with pruning, covered by tests.
4. Implement the pipeline — queue, stability wait, transformation set, retry, outcome classification, counters — against fakes, covered by tests.
5. Implement the session abstraction over the existing client, honouring validity.
6. Wire the pipeline into the host as a hosted service.
7. Supply WPF implementations of the notifier, folder picker, and dispatcher.
8. Reduce `MainViewModel` to log display and counters; rebind `SettingsViewModel` to the store; delete `PersistAndRestoreService`.
9. Verify end to end against a real Dropbox folder: truncation, startup scan, baseline, retry, duplicate, deletion, live settings changes.

**Rollback:** revert the branch. The previous settings file is preserved with a backup suffix at migration time, so the reverted application reads it unchanged. The processed-file record is simply ignored by the old code.

## Open Questions

- What quiet interval and consecutive-check count strike the right balance between latency and safety for Dropbox writes? To be settled empirically in step 9, then fixed in the spec.
- Should an explicit "import files already present" action exist, for the new user whose files were baselined? Deferred — it is a user interface feature and belongs in `avalonia-ui-port` if it is wanted at all.
- How many entries should the processed-file record retain, and should pruning be by count or by age?
- Should a duplicate response eventually be allowed to delete the source file, perhaps behind a setting? Left conservative for now.
