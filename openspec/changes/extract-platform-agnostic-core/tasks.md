## 1. Preconditions

- [x] 1.1 Confirm `modernize-dotnet10-foundation` is merged: .NET 10, central package management, `ILogger<T>` available in the core library, test project in place
- [x] 1.2 Create branch `refactor/platform-agnostic-core`
- [ ] 1.3 Collect sample `.fit` files for tests, including one real Wahoo export. Not needed for the pipeline's own tests, which operate on arbitrary bytes because nothing here parses FIT content — that arrives with `fit-device-emulation`. Required for the end-to-end verification in group 10
- [x] 1.4 Record the current log message wording emitted during startup, detection, connection, and upload, so it can be preserved — captured by `modernize-dotnet10-foundation` in `baseline-log-messages.md`

## 2. Abstractions

- [x] 2.1 Define the settings store interface: read typed settings, update, change notification
- [x] 2.2 Define the processed-activity record interface: check, mark, prune
- [x] 2.3 Define the Garmin session interface: obtain a valid session, renewing when expired
- [x] 2.4 Define the activity transformation interface, applied to file content before upload
- [x] 2.5 Define the notifier interface
- [x] 2.6 Define the folder picker interface
- [x] 2.7 Define the user interface dispatcher interface
- [x] 2.8 Confirm every new interface lives in the core library and none references a user interface type

## 3. Settings store

- [x] 3.1 Define the typed user settings object: watched folder, Garmin credentials, keep-uploaded-file, theme
- [x] 3.2 Split `AppConfig` so that shipped configuration and user settings no longer share a type
- [x] 3.3 Implement the store: load on start, persist on change, raise change notification
- [x] 3.4 Implement migration from the previous base64-wrapped file, preserving the original under a backup name
- [x] 3.5 Fall back to defaults and log when a stored value cannot be parsed
- [x] 3.6 Log and continue when settings cannot be written
- [x] 3.7 Test: previous-format settings migrate with all five values intact
- [x] 3.8 Test: the previous file is preserved as a backup
- [x] 3.9 Test: migration does not run a second time
- [x] 3.10 Test: a missing previous file is not reported as a failure
- [x] 3.11 Test: an unparseable value falls back to its default without crashing
- [x] 3.12 Test: a changed setting is readable after simulated abrupt termination

## 4. Processed-activity record

- [x] 4.1 Implement content hashing of activity bytes
- [x] 4.2 Implement the durable record: hash, original file name, outcome, timestamp
- [x] 4.3 Implement pruning so the record stays bounded
- [x] 4.4 Implement first-run detection based on the absence of the record
- [x] 4.5 Test: the same content under a different name is recognised as already processed
- [x] 4.6 Test: the record survives a restart
- [x] 4.7 Test: pruning keeps the record bounded
- [x] 4.8 Test: a missing record does not crash and results in re-offering the file

## 5. Readiness detection

- [x] 5.1 Implement stability polling on length and last-write time across consecutive checks
- [x] 5.2 Add a read-only open as secondary confirmation, not as the primary signal
- [x] 5.3 Implement the readiness timeout, after which the file is abandoned and reported
- [x] 5.4 Verify no code path depends on acquiring an exclusive lock
- [x] 5.5 Test: a file growing over several intervals is not read until it stops
- [x] 5.6 Test: a file complete on arrival is processed after the quiet interval
- [x] 5.7 Test: a file that never stabilises is abandoned with a logged reason and no upload

## 6. Pipeline

- [x] 6.1 Implement the in-memory queue, with the discovery callback only enqueueing
- [x] 6.2 Implement the single serial worker
- [x] 6.3 Implement the processing sequence: readiness, read, hash, idempotence check, transformation set, upload, outcome, retention
- [x] 6.4 Ship the transformation set empty, verifying uploaded bytes are identical to file content
- [x] 6.5 Implement outcome classification: success, duplicate, transient failure, permanent failure
- [x] 6.6 Implement retry with exponential backoff and jitter, bounded attempts, transient failures only
- [x] 6.7 Implement single renewal and single retry on an unauthorised response
- [x] 6.8 Implement retention: delete only on confirmed success with retention off; retain on duplicate, failure, and retention on
- [x] 6.9 Implement the processed, failed, and duplicate counters with change notification
- [x] 6.10 Ensure an unexpected exception is logged, counted as a failure, and does not stop the worker
- [ ] 6.11 Preserve the log message wording recorded in 1.4
- [x] 6.12 Test: transient failure retried then succeeding is counted as processed
- [x] 6.13 Test: retries stop at the configured maximum
- [x] 6.14 Test: a client error is not retried
- [x] 6.15 Test: an unauthorised response triggers renewal then one retry
- [x] 6.16 Test: a duplicate is counted as duplicate, not retried, and the file is retained
- [x] 6.17 Test: a failure retains the source file
- [x] 6.18 Test: an exception in one activity does not prevent the next from being processed
- [x] 6.19 Test: files queued during an in-flight upload are processed afterwards, in order

## 7. Discovery

- [x] 7.1 Implement the watcher source: `.fit` filter, non-recursive, enqueue only
- [x] 7.2 Implement the startup scan
- [x] 7.3 Start the watcher before the startup scan so nothing arriving during the scan is lost
- [x] 7.4 Implement first-run baseline: record existing files as processed without uploading
- [x] 7.5 Log the number of files baselined
- [x] 7.6 Restart the watcher when the watched folder setting changes
- [x] 7.7 Log and keep running when no folder is configured, and start watching as soon as one is supplied
- [x] 7.8 Log and keep running when the configured folder does not exist
- [x] 7.9 Test: a file seen by both the scan and the watcher is processed exactly once
- [x] 7.10 Test: first run baselines existing files with no upload
- [x] 7.11 Test: the second run processes a file that arrived after the baseline
- [x] 7.12 Test: changing the watched folder moves watching without a restart

## 8. Session

- [ ] 8.1 Implement the session abstraction over the existing Garmin client
- [ ] 8.2 Honour the client's existing validity property instead of testing for a non-null token
- [ ] 8.3 Renew before upload when the session is invalid
- [ ] 8.4 Log the reason and count a failure when renewal fails, retaining the source file
- [ ] 8.5 Test: an expired session is renewed before the upload
- [ ] 8.6 Test: a valid session is reused without re-authenticating

## 9. Host wiring and user interface

- [ ] 9.1 Register the pipeline as a hosted service in the existing generic host
- [ ] 9.2 Register the settings store, record, session, and abstractions in the container
- [x] 9.3 Implement the WPF notifier over the existing toast service
- [x] 9.4 Implement the WPF folder picker over the existing folder dialog
- [x] 9.5 Implement the WPF dispatcher over the existing application dispatcher
- [ ] 9.6 Reduce `MainViewModel` to log display and counters; remove watcher construction, authentication, upload, and deletion
- [x] 9.7 Bind `SettingsViewModel` to the settings store; remove the `App.Current.Properties` writes
- [x] 9.8 Delete `PersistAndRestoreService` and its interface
- [ ] 9.9 Remove the "restart the application to apply" message and any other restart instruction
- [ ] 9.10 Verify no view model constructs a watcher, reads files, authenticates, uploads, or deletes

## 10. End-to-end verification

- [ ] 10.1 Verify migration from a real previous-version settings file
- [ ] 10.2 Verify first launch baselines existing files and reports the count
- [ ] 10.3 Verify a file copied into the folder is detected, uploaded complete, and counted
- [ ] 10.4 Verify a large file written slowly is not uploaded truncated
- [ ] 10.5 Verify a file present at startup after a restart is processed once
- [ ] 10.6 Verify re-offering an already-processed file results in no second upload
- [ ] 10.7 Verify retention on and off against the real folder
- [ ] 10.8 Verify a duplicate response is reported as duplicate and the file is retained
- [ ] 10.9 Verify an authentication failure appears in the log and the failure counter
- [ ] 10.10 Verify changing the watched folder and the retention option takes effect without a restart
- [ ] 10.11 Verify counters reset on restart
- [ ] 10.12 Compare emitted log messages against the 1.4 baseline

## 11. Settle open questions

- [ ] 11.1 Determine the quiet interval and consecutive-check count empirically against a real Dropbox folder, then record the chosen values in the spec
- [ ] 11.2 Decide the record retention strategy — by count or by age — and record it
- [ ] 11.3 Record whether an explicit "import files already present" action should be raised as follow-up work for `avalonia-ui-port`
- [ ] 11.4 Record the decision to keep duplicates from deleting the source file, so it is not revisited by accident
