## ADDED Requirements

### Requirement: Durable log file in the user data directory

The application SHALL write diagnostic log entries to a file located under the current user's local application data directory, in a `Logs` folder alongside the existing configuration folder. The application SHALL NOT write log files to the process working directory.

#### Scenario: Log file is created under local application data

- **WHEN** the application starts
- **THEN** a log file is created under the user's local application data directory, in the application's `Logs` folder

#### Scenario: No log file is written to the working directory

- **WHEN** the application is launched from an arbitrary working directory and produces log entries
- **THEN** no log file is created in that working directory

#### Scenario: Missing log directory is created

- **WHEN** the application starts and the `Logs` folder does not exist
- **THEN** the folder is created and logging proceeds without error

### Requirement: Bounded, rotating log files

Log files SHALL roll daily, SHALL roll again when a single file reaches 10 MB, and the application SHALL retain at most 7 log files, deleting the oldest beyond that limit. These limits are fixed constants and are not user-configurable.

#### Scenario: A new file is started each day

- **WHEN** log entries are written on two different calendar days
- **THEN** the entries are written to two separate files distinguished by date

#### Scenario: A file is rolled when it reaches the size limit

- **WHEN** a single log file reaches 10 MB
- **THEN** a new file is started and subsequent entries are written to it

#### Scenario: Retention limit is enforced

- **WHEN** more than 7 log files exist
- **THEN** the oldest files are deleted so that at most 7 remain

#### Scenario: Disk usage stays bounded over long-running use

- **WHEN** the application runs continuously for an extended period
- **THEN** total log disk usage never exceeds the retention count multiplied by the per-file size limit

### Requirement: Logging does not block the user interface

Log writes SHALL NOT perform synchronous file input/output on the user interface thread. Constructing a log entry object SHALL NOT itself write to disk.

#### Scenario: Log entry construction performs no file access

- **WHEN** a log entry object is constructed
- **THEN** no file is opened, created, or written as a side effect

#### Scenario: User interface stays responsive under log volume

- **WHEN** a burst of log entries is emitted while the user interacts with the window
- **THEN** the interface remains responsive and no interaction is blocked waiting on disk

#### Scenario: A failing log sink does not crash the application

- **WHEN** the log file cannot be written, for example because the directory is read-only
- **THEN** the application continues to run and the failure does not propagate to the user interface

### Requirement: Single logging pipeline shared by file and in-app viewer

The application SHALL emit all diagnostic messages through one logging abstraction. The in-app log viewer and the log file SHALL be two sinks of that single pipeline, so that a message recorded in one is recorded in the other. Messages originating in the core library SHALL reach both sinks.

#### Scenario: A message appears in both sinks

- **WHEN** any component emits a log message
- **THEN** the message appears both in the in-app log viewer and in the log file

#### Scenario: Core library messages reach the viewer

- **WHEN** the core library logs an upload failure
- **THEN** the message is visible in the in-app log viewer, not only in the file

#### Scenario: Existing operational messages are preserved

- **WHEN** the application starts, detects a new file, connects to Garmin, and uploads an activity
- **THEN** the same operational messages as the previous version are emitted, in the same order

### Requirement: Bounded in-app log viewer

The collection backing the in-app log viewer SHALL retain a bounded number of entries. When the limit is reached, the oldest entries SHALL be discarded so that memory use does not grow without limit during a long-running session.

#### Scenario: Oldest entries are trimmed at the cap

- **WHEN** the number of emitted log entries exceeds the retention cap
- **THEN** the collection holds at most the cap, containing the most recent entries

#### Scenario: Trimming does not affect the log file

- **WHEN** entries are trimmed from the in-app viewer
- **THEN** those entries remain present in the log file

#### Scenario: Viewer updates are marshalled to the user interface thread

- **WHEN** a log message is emitted from a background thread
- **THEN** the entry is appended to the observable collection on the user interface thread without a threading exception

### Requirement: Unhandled exceptions are recorded

The application SHALL record unhandled exceptions, including the exception type, message, and stack trace, before the application terminates. Unhandled exceptions SHALL NOT fail silently.

#### Scenario: An unhandled dispatcher exception is logged

- **WHEN** an unhandled exception reaches the application dispatcher
- **THEN** its type, message, and stack trace are written to the log file

#### Scenario: The handler is not empty

- **WHEN** the unhandled exception handler is inspected
- **THEN** it contains logging logic rather than an empty body

### Requirement: Exception stack traces are preserved on rethrow

Code that catches an exception and rethrows it SHALL preserve the original stack trace. Rethrowing in a way that resets the stack trace SHALL NOT be used.

#### Scenario: Upload failure retains its origin

- **WHEN** an exception raised inside the Garmin upload path is caught and rethrown
- **THEN** the logged stack trace still identifies the original throw site

#### Scenario: No stack-trace-resetting rethrow remains

- **WHEN** the source is inspected for rethrow statements
- **THEN** no `throw ex;` form is present where `throw;` is required

### Requirement: Severity levels are recorded

Each log entry SHALL carry a severity level, and the level SHALL be present in the written log file so that errors can be distinguished from informational messages.

#### Scenario: Level is written to the file

- **WHEN** an informational message and an error message are emitted
- **THEN** each line in the log file identifies its severity level

#### Scenario: Errors are distinguishable

- **WHEN** the log file is searched for failures
- **THEN** error entries can be isolated by their severity level without parsing message text
