## MODIFIED Requirements

### Requirement: Session validity is checked before every upload

The pipeline SHALL obtain a valid authenticated session before each upload, renewing it when the current one has expired. The presence of a session object SHALL NOT be treated as proof that it is still usable. A renewal that cannot succeed without the user signing in again SHALL be treated as a distinct condition rather than as an ordinary failure: the pipeline SHALL pause, the queued activity SHALL remain queued, and processing SHALL resume once the user has signed in.

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

- **WHEN** the session cannot be renewed for a transient reason such as an unavailable network
- **THEN** the failure is logged with its reason, the activity is counted as failed, and the source file is retained

#### Scenario: Unrecoverable renewal pauses the pipeline

- **WHEN** renewal fails because the stored credentials are no longer valid
- **THEN** the pipeline pauses, the activity remains queued rather than being counted as failed, and the user is told that signing in again is required

#### Scenario: Paused work resumes after signing in

- **WHEN** the user signs in again while the pipeline is paused
- **THEN** processing resumes and the queued activity is uploaded

#### Scenario: A pause does not inflate the failure count

- **WHEN** several activities are queued and the pipeline pauses for re-authentication
- **THEN** none of them is counted as failed

### Requirement: Transient failures are retried, permanent failures are not

The pipeline SHALL retry uploads that fail for transient reasons, using exponential backoff and a bounded number of attempts. Failures that cannot succeed on retry SHALL be reported immediately without further attempts. When a response states how long to wait before retrying, the pipeline SHALL honour that delay instead of its own backoff schedule.

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

#### Scenario: A stated retry delay is honoured

- **WHEN** an upload is rejected with a rate-limit response carrying a retry delay
- **THEN** the pipeline waits at least that long before the next attempt, rather than using its own backoff interval

#### Scenario: Rate limiting does not escalate

- **WHEN** consecutive uploads are rate-limited
- **THEN** the pipeline does not increase its request rate, and the condition is logged distinctly from a server error
