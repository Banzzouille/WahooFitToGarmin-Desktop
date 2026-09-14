## 1. Validation gate

- [ ] 1.1 Confirm `extract-platform-agnostic-core` is merged, so the session abstraction and the pipeline exist
- [ ] 1.2 Run the sign-in request by hand **from a residential connection**, not from a server or a container, and record the status and response shape
- [ ] 1.3 **STOP AND RE-PLAN if the sign-in endpoint answers with a rate-limit status** — the premise that desktop clients are not blocked would be false, and the cancelled web-view approach returns as the candidate
- [ ] 1.4 Record which client identifier the token exchange accepts, and the lifetimes reported for both tokens
- [ ] 1.5 Run a refresh by hand and record whether a new refresh token is returned
- [ ] 1.6 Upload a real `.fit` file with the resulting access token and record the status and response shape
- [ ] 1.7 **STOP AND RE-PLAN if the upload is rejected** — the token would not be sufficient for uploads
- [ ] 1.8 Temporarily enable two-step verification on the test account and repeat sign-in, recording the response shape, the reported method, and the verification step's outcome
- [ ] 1.9 Confirm whether the existing `UploadResponse` model still matches the upload response
- [ ] 1.10 Record all findings with values and error codes only — never token strings
- [ ] 1.11 Create branch `feat/garmin-mobile-auth`

## 2. Sign-in client

- [ ] 2.1 Define the client identity constants in one place — client identifier, service, locale, user agent — and reference them from every request
- [ ] 2.2 Implement the sign-in request with a JSON body and the mobile headers
- [ ] 2.3 Create the cookie container per authentication attempt and share it between sign-in and code verification
- [ ] 2.4 Classify the three response shapes: ticket returned, second factor required, credentials rejected
- [ ] 2.5 Surface the second-factor method reported by the service
- [ ] 2.6 Report a rate-limit response as its own condition, distinct from rejected credentials
- [ ] 2.7 Implement the code verification request on the same cookie container, asking the service to remember the session
- [ ] 2.8 Discard the password as soon as the sign-in request completes, on both the success and failure paths
- [ ] 2.9 Test: a ticket response yields a ticket
- [ ] 2.10 Test: a second-factor response surfaces the reported method and does not yield a ticket
- [ ] 2.11 Test: rejected credentials produce the credentials error, not a generic failure
- [ ] 2.12 Test: a rate-limit response produces its own error
- [ ] 2.13 Test: the verification request carries the cookies set by sign-in
- [ ] 2.14 Test: no password appears in any request that is repeated after a failure

## 3. Token exchange and renewal

- [ ] 3.1 Implement the token exchange, sending the constant service value and never a value taken from the sign-in response
- [ ] 3.2 Implement the client identifier fallback chain in order, first acceptance winning
- [ ] 3.3 Persist the accepted client identifier with the tokens
- [ ] 3.4 Implement refresh using the persisted identifier
- [ ] 3.5 Persist the returned token pair before any request uses it
- [ ] 3.6 Serialise refresh so two triggers collapse into one in-flight attempt
- [ ] 3.7 Trigger refresh ahead of expiry rather than after a failure
- [ ] 3.8 Classify token endpoint failures on the service's error code alone; treat the dead-credential code as "signing in again required"
- [ ] 3.9 Test: a later identifier is used when earlier ones are rejected
- [ ] 3.10 Test: refresh presents the recorded identifier, not a default
- [ ] 3.11 Test: the new token pair is written to storage before it is used
- [ ] 3.12 Test: concurrent demands for a renewed session produce one renewal
- [ ] 3.13 Test: the dead-credential error maps to "signing in again required" and not to a transient failure
- [ ] 3.14 Test: a network error during renewal does not map to "signing in again required"

## 4. Redaction and logging

- [ ] 4.1 Implement redaction of token-shaped values in any text destined for a log
- [ ] 4.2 Ensure token endpoint response bodies are never logged verbatim
- [ ] 4.3 Log authentication state transitions without any credential
- [ ] 4.4 Test: a body containing a token-shaped value is redacted before reaching the log
- [ ] 4.5 Test: a failed token request logs the status and error code and not the body
- [ ] 4.6 Test: a complete sign-in, renewal, and upload cycle produces a log containing no token, ticket, or password

## 5. Credential storage

- [ ] 5.1 Define the secret store abstraction in the core library
- [ ] 5.2 Implement the Windows protection facility behind a runtime platform check
- [ ] 5.3 Implement the macOS keychain behind the same runtime check
- [ ] 5.4 Confirm no operating-system-specific target framework was introduced
- [ ] 5.5 Degrade to "signing in again required" when the store cannot be read or decrypted
- [ ] 5.6 Discard unusable stored data rather than retrying it
- [ ] 5.7 Implement disconnect: remove stored credentials and return to the signed-out state
- [ ] 5.8 Test: stored credentials round-trip through the abstraction
- [ ] 5.9 Test: an unreadable store produces "signing in again required" and does not crash
- [ ] 5.10 Test: disconnect leaves no credential in storage
- [ ] 5.11 Verify on Windows that stored credentials are not readable as text
- [ ] 5.12 Record that macOS keychain behaviour remains unverified until the macOS pass of `avalonia-ui-port`

## 6. Session implementation

- [ ] 6.1 Implement the session abstraction from `extract-platform-agnostic-core` over the new flow
- [ ] 6.2 Report the three states the pipeline needs: connected, transient failure, signing in again required
- [ ] 6.3 Remove the old client's authentication members from its interface
- [ ] 6.4 Test: an expired session is renewed before the upload
- [ ] 6.5 Test: a valid session is reused without renewing

## 7. Pipeline integration

- [ ] 7.1 Pause the pipeline when signing in again is required, leaving the current activity queued
- [ ] 7.2 Resume processing automatically after a successful sign-in
- [ ] 7.3 Ensure a pause does not increment the failure count
- [ ] 7.4 Honour a stated retry delay on a rate-limited upload instead of the generic backoff
- [ ] 7.5 Log rate limiting distinctly from a server error, and never increase the request rate in response
- [ ] 7.6 Test: an unrecoverable renewal pauses rather than fails
- [ ] 7.7 Test: several queued activities produce no failures when the pipeline pauses
- [ ] 7.8 Test: signing in again resumes the queue and the activity uploads
- [ ] 7.9 Test: a rate-limit response with a delay is respected

## 8. Upload path

- [ ] 8.1 Send the access token as a bearer credential with the shared client identity
- [ ] 8.2 Classify upload outcomes: success, duplicate, unauthorised, rate-limited, other client error, server error
- [ ] 8.3 Adjust the response model if task 1.9 found a mismatch
- [ ] 8.4 Test: each outcome maps to the expected pipeline result

## 9. User interface

- [ ] 9.1 Add a sign-in view with email and password fields and a connection state display
- [ ] 9.2 Add a verification code prompt that states the method and does not require re-entering the password
- [ ] 9.3 Show the "signing in again required" state prominently when the pipeline is paused
- [ ] 9.4 Add a disconnect action
- [ ] 9.5 Remove the stored password field from the settings page
- [ ] 9.6 Ensure the password field is never pre-filled from storage, because nothing is stored

## 10. Purge and cleanup

- [ ] 10.1 Remove the password from the migrated settings object
- [ ] 10.2 Delete the previous migration's backup file when it contains credentials, rather than keeping it
- [ ] 10.3 Write the settings file as plain readable JSON, dropping the obfuscation wrapper
- [ ] 10.4 Log that a purge occurred without revealing what was purged
- [ ] 10.5 Delete the old authentication implementation, the magic strings, the client factory, and the obsolete data transfer objects
- [ ] 10.6 Delete the consumer key download and its URL entry
- [ ] 10.7 Remove the OAuth1 signing dependency
- [ ] 10.8 Remove the obsolete endpoint entries from the URL list
- [ ] 10.9 Test: a previous settings file containing a password is migrated without the password and its source file is deleted
- [ ] 10.10 Test: a previous settings file without credentials is preserved as a backup

## 11. End-to-end verification

- [ ] 11.1 Sign in with two-step verification disabled and upload a real activity
- [ ] 11.2 Restart the application and confirm no sign-in is required
- [ ] 11.3 Force a renewal and confirm the upload still succeeds afterwards
- [ ] 11.4 Invalidate the stored credentials, confirm the pipeline pauses and the interface states that signing in again is required
- [ ] 11.5 Sign in again and confirm the queued activity uploads
- [ ] 11.6 Temporarily enable two-step verification, sign in with the code, and upload
- [ ] 11.7 Disconnect and confirm the next file reports that signing in is required
- [ ] 11.8 Inspect the settings file and confirm it contains no secret and is readable JSON
- [ ] 11.9 Inspect the log file from the whole session and confirm it contains no token, ticket, or password

## 12. Attribution and follow-up

- [ ] 12.1 Attribute the reconstructed flow in the source file that implements it, naming the upstream projects and their licence
- [ ] 12.2 Record the observed token lifetimes so the user-facing message about how often signing in is needed is accurate
- [ ] 12.3 Record whether asking the service to remember the session reduces how often a code is demanded, as an observation over time rather than a gate
- [ ] 12.4 Note for `documentation-overhaul` that the README's clear-text storage warning is now false and what replaces it
- [ ] 12.5 Note for `avalonia-ui-port` that the sign-in and verification views must be ported, and that no password field is stored
