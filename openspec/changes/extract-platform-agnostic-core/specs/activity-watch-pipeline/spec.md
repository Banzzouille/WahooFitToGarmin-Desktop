## ADDED Requirements

### Requirement: Activity discovery is independent of the user interface

Discovery and processing of activity files SHALL be performed by the core library and SHALL NOT depend on any user interface framework. The pipeline SHALL be exercisable in automated tests without a window, a real file system watcher, or a network connection.

#### Scenario: Pipeline runs without a user interface

- **WHEN** the pipeline is started in a test host with no user interface
- **THEN** it discovers, processes, and reports on files exactly as it does in the application

#### Scenario: No application logic remains in view models

- **WHEN** the view models are inspected
- **THEN** none of them constructs a file watcher, reads files, authenticates, uploads, or deletes files

### Requirement: New files in the watched folder are detected

The pipeline SHALL detect `.fit` files appearing in the configured folder while the application is running. Files in subfolders SHALL NOT be processed, matching the current behaviour.

#### Scenario: A new file is detected

- **WHEN** a `.fit` file appears in the watched folder
- **THEN** the pipeline queues it for processing and records the detection in the log

#### Scenario: Files of other types are ignored

- **WHEN** a file that is not a `.fit` file appears in the watched folder
- **THEN** the pipeline ignores it

#### Scenario: Subfolders are not watched

- **WHEN** a `.fit` file appears in a subfolder of the watched folder
- **THEN** the pipeline does not process it

#### Scenario: Detection does not block

- **WHEN** a file is detected
- **THEN** the detection path performs no file reading or network access, queueing the work instead

### Requirement: Files are only read once writing has finished

The pipeline SHALL NOT read an activity file until its content has stopped changing. A file SHALL be considered ready only after its length and last-write time remain unchanged across consecutive checks separated by a quiet interval, and it can be opened for reading.

The quiet interval SHALL be one second, at least two consecutive unchanged observations SHALL be required, and a file still changing after two minutes SHALL be abandoned. These are fixed constants rather than settings: they are not values a user can reason about, and the point of them is that the behaviour is predictable. They were chosen against the write pattern of a desktop sync client and remain subject to confirmation against a real folder on each supported platform.

#### Scenario: A file still being written is not uploaded

- **WHEN** a file is created and then continues to grow for several seconds
- **THEN** the pipeline waits, and uploads only the complete content

#### Scenario: A file that appears complete is processed promptly

- **WHEN** a file is created by a rename and its content never changes afterwards
- **THEN** the pipeline processes it once the quiet interval has elapsed

#### Scenario: A file that never stabilises is abandoned

- **WHEN** a file keeps changing beyond the readiness timeout
- **THEN** the pipeline stops waiting, records a failure with the reason, and does not upload partial content

#### Scenario: Readiness does not rely on exclusive locking

- **WHEN** the readiness check runs on a platform where file locks are advisory
- **THEN** readiness is still determined correctly, because it is based on content stability rather than on acquiring an exclusive lock

### Requirement: Files present at startup are processed

The pipeline SHALL scan the watched folder when the application starts, so that files which arrived while the application was closed are processed rather than ignored.

#### Scenario: A file that arrived while closed is uploaded

- **WHEN** the application starts and the watched folder contains a `.fit` file that has never been processed
- **THEN** the pipeline processes it

#### Scenario: Startup scan and live watching do not conflict

- **WHEN** a file appears during the startup scan
- **THEN** it is processed exactly once, whether it was seen by the scan, by the watcher, or by both

### Requirement: First run establishes a baseline instead of uploading history

On the first run after this capability is introduced, every activity file already present in the watched folder SHALL be recorded as already processed and SHALL NOT be uploaded. The number of files baselined SHALL be reported in the log.

#### Scenario: Existing files are not mass-uploaded

- **WHEN** the application starts for the first time after the upgrade and the watched folder contains files from previous months
- **THEN** no upload is attempted for them, and each is recorded as already processed

#### Scenario: The baseline is reported

- **WHEN** files are baselined
- **THEN** the log states how many files were marked as already processed

#### Scenario: Subsequent startups scan normally

- **WHEN** the application starts a second time and a new file has arrived since the baseline
- **THEN** that file is processed

### Requirement: An activity is never processed twice

The pipeline SHALL maintain a durable record of processed activities keyed on file content, so that the same activity is not uploaded twice regardless of its path, its name, or how it was discovered.

#### Scenario: The same file offered twice is processed once

- **WHEN** a file is processed, then removed and restored to the folder with a different name
- **THEN** the pipeline recognises it as already processed and does not upload it again

#### Scenario: The record survives a restart

- **WHEN** the application is restarted after processing a file that was retained on disk
- **THEN** the startup scan does not upload it again

#### Scenario: The record does not grow without bound

- **WHEN** a large number of activities have been processed over time
- **THEN** the record is pruned and remains bounded in size

#### Scenario: Pruning is by count, oldest first

- **WHEN** the record exceeds 2000 entries
- **THEN** the oldest entries are removed until it holds 2000, and the most recently recorded activities are retained

#### Scenario: A lost record degrades safely

- **WHEN** the record is deleted and a previously uploaded file is offered again
- **THEN** the pipeline uploads it, the service reports it as a duplicate, and the file is not deleted

### Requirement: Source file retention follows the user's choice

After a confirmed successful upload, the pipeline SHALL delete the source file when the keep-uploaded-file option is disabled, and retain it when the option is enabled. The source file SHALL NOT be deleted for any other outcome, including a duplicate report or a failure.

#### Scenario: File is deleted after success when retention is off

- **WHEN** an upload succeeds and the keep-uploaded-file option is disabled
- **THEN** the source file is deleted

#### Scenario: File is retained after success when retention is on

- **WHEN** an upload succeeds and the keep-uploaded-file option is enabled
- **THEN** the source file remains in the folder

#### Scenario: File is retained on duplicate

- **WHEN** the service reports the activity as already present
- **THEN** the source file is retained regardless of the retention option

#### Scenario: File is retained on failure

- **WHEN** an upload fails
- **THEN** the source file is retained so that the activity is not lost

### Requirement: A transformation step exists between reading and uploading

The pipeline SHALL pass the file's content through an ordered, possibly empty, set of transformations before uploading. Adding a transformation SHALL NOT require restructuring the pipeline.

#### Scenario: An empty transformation set uploads the original content

- **WHEN** no transformation is configured
- **THEN** the bytes uploaded are byte-for-byte identical to the file's content

#### Scenario: A configured transformation is applied before upload

- **WHEN** a transformation is configured
- **THEN** the uploaded content is the transformed content, and the source file on disk is unchanged
