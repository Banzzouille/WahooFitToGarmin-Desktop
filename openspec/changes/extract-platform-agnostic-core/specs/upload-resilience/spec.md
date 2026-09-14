## ADDED Requirements

### Requirement: Session validity is checked before every upload

The pipeline SHALL obtain a valid authenticated session before each upload, renewing it when the current one has expired. The presence of a session object SHALL NOT be treated as proof that it is still usable.

#### Scenario: An expired session is renewed

- **WHEN** an upload is attempted after the current session has passed its expiry
- **THEN** the session is renewed before the upload is sent, and the upload succeeds

#### Scenario: A valid session is reused

- **WHEN** consecutive uploads occur while the session remains valid
- **THEN** the session is reused and no additional authentication is performed

#### Scenario: Expiry is judged on validity, not on object presence

- **WHEN** a session object exists but its expiry has passed
- **THEN** it is treated as unusable and renewed

#### Scenario: Renewal failure is reported

- **WHEN** the session cannot be renewed
- **THEN** the failure is logged with its reason, the activity is counted as failed, and the source file is retained

### Requirement: Transient failures are retried, permanent failures are not

The pipeline SHALL retry uploads that fail for transient reasons, using exponential backoff and a bounded number of attempts. Failures that cannot succeed on retry SHALL be reported immediately without further attempts.

#### Scenario: A network failure is retried

- **WHEN** an upload fails with a network error or a server error
- **THEN** the pipeline retries after a delay, increasing the delay between attempts

#### Scenario: Retries are bounded

- **WHEN** every attempt fails
- **THEN** the pipeline stops after the configured maximum number of attempts and reports a failure

#### Scenario: An unauthorised response triggers renewal rather than blind retry

- **WHEN** an upload is rejected as unauthorised
- **THEN** the session is renewed and the upload is attempted once more

#### Scenario: A client error is not retried

- **WHEN** an upload fails with a client error that is not an authorisation or duplicate response
- **THEN** the pipeline reports the failure without retrying

#### Scenario: A successful retry is reported as success

- **WHEN** an upload fails once and succeeds on the next attempt
- **THEN** the activity is counted as processed, not as failed

### Requirement: Duplicates are a distinct outcome

An upload rejected because the service already holds the activity SHALL be reported as a duplicate — neither a success nor a failure. It SHALL NOT be retried.

#### Scenario: A duplicate is recognised

- **WHEN** the service reports that the activity already exists
- **THEN** the outcome is recorded as a duplicate and stated as such in the log

#### Scenario: A duplicate is not retried

- **WHEN** a duplicate response is received
- **THEN** no further upload attempt is made for that activity

#### Scenario: A duplicate is not counted as a failure

- **WHEN** an activity is reported as a duplicate
- **THEN** the failure count is unchanged and the duplicate count increases

### Requirement: No failure is silent

Every failure in the pipeline SHALL be logged and reflected in the counters. A failure SHALL NOT be lost because the work was started without being awaited or because it occurred outside an error handler.

#### Scenario: An authentication failure is visible

- **WHEN** authentication fails while processing a file
- **THEN** the failure appears in the log with its reason and the failure count increases

#### Scenario: An upload failure is visible

- **WHEN** an upload fails for any reason
- **THEN** the failure appears in the log with the file name and the reason, and the failure count increases

#### Scenario: An unexpected exception does not stop the pipeline

- **WHEN** processing one activity throws an unexpected exception
- **THEN** the exception is logged, that activity is counted as failed, and the next queued activity is still processed

### Requirement: Activities are processed one at a time

The pipeline SHALL process queued activities serially, so that log output remains in a comprehensible order and simultaneous authentication attempts are avoided.

#### Scenario: A burst of files is processed in order

- **WHEN** several files appear at once
- **THEN** they are processed one after another, and the log reflects that order

#### Scenario: A slow upload does not block detection

- **WHEN** an upload is in progress and another file appears
- **THEN** the new file is queued immediately and processed after the current one

### Requirement: Outcome counters are exposed

The pipeline SHALL expose counts of processed, failed, and duplicate activities for the current session, updated as each activity reaches its outcome.

#### Scenario: Counters reflect outcomes

- **WHEN** activities have succeeded, failed, and been reported as duplicates
- **THEN** the three counts match the number of activities in each outcome

#### Scenario: Counters are observable by the user interface

- **WHEN** a counter changes
- **THEN** the user interface is notified and can display the new value without polling

#### Scenario: Counters describe the current session

- **WHEN** the application is restarted
- **THEN** the counters start from zero
