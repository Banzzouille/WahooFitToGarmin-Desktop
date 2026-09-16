## ADDED Requirements

### Requirement: The user signs in with their Garmin credentials

The application SHALL let the user sign in on Garmin's own sign-in page, presented in an embedded web view. On that path the application SHALL NOT read, store, or transmit the user's password, and SHALL obtain only the service ticket resulting from a successful sign-in.

The application SHALL retain a fallback in which the user enters their Garmin email address and password in the application itself, for use when the web view cannot run. Both paths SHALL produce the same session, and no part of the application beyond sign-in SHALL depend on which path produced it.

Signing in SHALL NOT require the user to open developer tools or copy any value out of a network trace, on either path.

#### Scenario: Successful sign-in establishes a session

- **WHEN** the user enters valid credentials and confirms
- **THEN** a session is established, its state is reported as connected, and uploads can proceed

#### Scenario: Rejected credentials are reported plainly

- **WHEN** the credentials are rejected
- **THEN** the application states that the email or password is wrong, and does not report a generic authentication failure

#### Scenario: Rejected credentials are not retried automatically

- **WHEN** sign-in fails because the credentials are wrong
- **THEN** the application makes no further sign-in attempt until the user acts

#### Scenario: A rate-limited sign-in is distinguishable

- **WHEN** the sign-in endpoint responds with a rate-limit status rather than rejecting the credentials
- **THEN** the application reports that Garmin is currently refusing sign-in attempts, distinctly from wrong credentials, and does not retry on a timer

#### Scenario: No credential appears in a request that is repeated

- **WHEN** any request is retried after a failure
- **THEN** that request carries no password

### Requirement: A second authentication factor is handled in the application

When a second factor is required, the web view path SHALL let Garmin conduct it on its own page, and the application SHALL NOT prompt for a code itself. Any challenge Garmin presents there, including a captcha or a passkey, SHALL be the user's to complete rather than something the application reimplements.

On the fallback path, when Garmin requires a second factor the application SHALL report that a code is needed, state which method Garmin used to send it, and accept the code from the user. The session started by the credential step SHALL be carried into the verification step.

#### Scenario: A code is requested

- **WHEN** the account has two-step verification enabled and credentials are accepted
- **THEN** the application asks for the verification code and states the method Garmin used

#### Scenario: A valid code completes sign-in

- **WHEN** the user supplies the correct code
- **THEN** the session is established and uploads can proceed

#### Scenario: An invalid code is reported without restarting sign-in

- **WHEN** the user supplies an incorrect code
- **THEN** the application states that the code is wrong and lets the user try again without re-entering the password

#### Scenario: Session continuity between the two steps

- **WHEN** the verification step runs
- **THEN** it carries the session established by the credential step, so Garmin accepts it

#### Scenario: An account without two-step verification is not prompted

- **WHEN** the account has no second factor configured
- **THEN** sign-in completes without asking for a code

### Requirement: The application presents one consistent client identity

Every request in the authentication and upload path SHALL present the same client identity. The application SHALL NOT identify itself as one client to obtain a session and as another to use it.

#### Scenario: Identity is consistent across the flow

- **WHEN** the sign-in, token exchange, and upload requests are inspected
- **THEN** they carry the same client identity

#### Scenario: Identity is defined in one place

- **WHEN** the source is inspected
- **THEN** the client identity values are declared once and referenced, not repeated per request

### Requirement: The token exchange records which client identifier succeeded

The application SHALL try its known client identifiers in order and use the first one Garmin accepts. The accepted identifier SHALL be persisted with the session and reused for every renewal.

#### Scenario: The first accepted identifier is used

- **WHEN** an earlier identifier in the list is rejected and a later one is accepted
- **THEN** the session is established with the accepted identifier

#### Scenario: Renewal reuses the recorded identifier

- **WHEN** the session is renewed
- **THEN** the request presents the identifier recorded when the session was established, not a hard-coded default

#### Scenario: All identifiers rejected is reported

- **WHEN** no identifier is accepted
- **THEN** the failure is reported to the user as a sign-in failure, with the error code from the service

### Requirement: No value from the sign-in response is echoed into the token exchange

The token exchange SHALL send constant values for the service it identifies. Values returned by the sign-in response SHALL NOT be fed back into the exchange request.

#### Scenario: Constant service value is used

- **WHEN** the token exchange request is inspected
- **THEN** the service value it carries is the application's constant, regardless of what the sign-in response contained

### Requirement: Sessions are renewed without user interaction

While renewal remains possible, the application SHALL renew the session on its own. Renewal SHALL be performed ahead of expiry rather than in response to a failed upload, SHALL never run concurrently with itself, and the renewed credentials SHALL be persisted before they are used.

#### Scenario: Renewal happens ahead of expiry

- **WHEN** the session approaches its expiry
- **THEN** it is renewed before an upload fails because of it

#### Scenario: Renewal is not performed twice at once

- **WHEN** two operations require a renewed session at the same time
- **THEN** a single renewal is performed and both operations use its result

#### Scenario: Renewed credentials are persisted before use

- **WHEN** a renewal returns new credentials
- **THEN** they are written to storage before any request uses them, so that a crash between the two cannot leave the stored credentials spent

#### Scenario: A renewal's predecessor is discarded

- **WHEN** a renewal succeeds
- **THEN** the credentials it replaced are no longer used for any subsequent renewal

#### Scenario: The user is not prompted while renewal works

- **WHEN** the application runs for weeks with renewal succeeding
- **THEN** the user is never asked to sign in

### Requirement: Expired authentication is a distinct, reported state

When renewal is no longer possible, the application SHALL enter a state that states signing in again is required. This state SHALL be distinguishable from a transient failure, SHALL be surfaced to the user, and SHALL be resolved by a successful sign-in.

#### Scenario: Expiry is reported, not retried

- **WHEN** renewal fails because the stored credentials are no longer valid
- **THEN** the application reports that signing in again is required and makes no further renewal attempt

#### Scenario: Expiry is distinguishable from a network failure

- **WHEN** renewal fails because the network is unavailable
- **THEN** the application does not report that signing in again is required

#### Scenario: Signing in again resolves the state

- **WHEN** the user signs in successfully after expiry
- **THEN** the state returns to connected and queued work resumes

### Requirement: The user can disconnect

The application SHALL let the user end the session. Disconnecting SHALL remove the stored credentials and return the application to a signed-out state.

#### Scenario: Disconnecting removes stored credentials

- **WHEN** the user disconnects
- **THEN** no session credential remains in storage

#### Scenario: Disconnecting stops uploads

- **WHEN** the user disconnects and a new activity file appears
- **THEN** the application reports that signing in is required rather than attempting an upload
