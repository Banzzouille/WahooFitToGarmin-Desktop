## Why

The application cannot log in to Garmin Connect. `Core/GARMIN/Client.Auth.cs` implements a seven-step scrape of the web sign-in pages — cookie priming, CSRF token extraction, a form POST, a redirect, a regular expression over HTML, an OAuth1 pre-authorization, then an OAuth1-to-OAuth2 exchange — and Garmin shut that path down in March 2026. Every other change in this modernization is cosmetic until this one lands: an application that cannot authenticate cannot upload.

The replacement is the flow Garmin's own mobile application uses: a JSON sign-in endpoint that returns a service ticket directly, exchanged for tokens that last about a month.

## What Changes

- **BREAKING**: remove the web sign-in scrape entirely — cookie priming on the embed page, CSRF extraction, the form POST, the service ticket regular expression, the OAuth1 pre-authorization, the OAuth1-to-OAuth2 exchange, and the old multi-factor endpoint. None of it can succeed any more.
- Replace it with the mobile sign-in flow: credentials are posted as JSON to Garmin's mobile sign-in endpoint, which returns a service ticket in its response body. No HTML is parsed and no regular expression is applied to a page.
- Exchange that ticket for a digital identity token pair — an access token valid about a day and a refresh token valid about a month — and upload with the access token as a bearer credential.
- Refresh without any user interaction while the refresh token lives, so the user signs in roughly once a month rather than every session.
- **Multi-factor authentication starts working.** `Client.CompleteMFAAuthAsync` exists today but no view ever calls it, so an account with two-step verification simply cannot connect. The new flow reports that a code is required, states which method Garmin used — email, text message, or authenticator application — and accepts the code in the application. The browser is never involved.
- Ask Garmin to remember the session when signing in and when verifying a code, so second-factor prompts become less frequent.
- **BREAKING**: the Garmin password is never written to disk. It is held in memory for the duration of the sign-in request and discarded. Any password stored by a previous version is purged, including the backup file left behind by the previous settings migration.
- Store the token pair encrypted at rest using each operating system's own facility.
- **BREAKING**: remove the runtime download of OAuth consumer keys from a third-party GitHub repository, eliminating a startup network dependency and a supply-chain exposure. The new flow uses a fixed set of client identifiers.
- Treat refresh tokens as single-use: whatever token the refresh response returns is persisted before it is relied upon, because the previous one is dead the moment it is spent.
- Report expiry as a distinct state. When the refresh token has expired, the application says that signing in again is required instead of failing uploads with an opaque error.
- Never log a response body from the token endpoint. Garmin echoes the submitted token back inside some error descriptions, so bodies are classified by their error code and any token-shaped string is redacted before anything reaches the log.
- Honour the `Retry-After` header when Garmin rate-limits an upload, instead of applying the generic backoff.
- Make the stored settings file plain readable JSON. `modernize-dotnet10-foundation` kept the base64 obfuscation wrapper only because the file still held a password; with no credential stored, the wrapper has nothing left to obscure.
- Supply the real implementation of the session abstraction introduced by `extract-platform-agnostic-core`, which until now wraps the old client.

This change is gated on manual validation. The endpoints and client identifiers are undocumented and reconstructed from observed mobile application traffic; they are confirmed by hand against a real account before any code is written.

## Capabilities

### New Capabilities

- `garmin-authentication`: how the user signs in, how a second factor is requested and supplied, how long a session lasts, how it is renewed without interaction, how expiry is surfaced, and how the session is discarded.
- `secret-storage`: how tokens are protected at rest on each operating system, and the guarantee that no password is ever written to disk or to a log.

### Modified Capabilities

- `upload-resilience`: the requirement that a session is renewed before every upload is extended. Renewal can now fail in a way no retry can fix — an expired refresh token requires the user to sign in again — which the existing requirement does not distinguish from a transient failure. A rate-limit response carrying a retry delay must also be honoured rather than retried on the generic schedule.
- `application-settings`: the password is removed from the stored settings object, the stored file's format changes from base64-wrapped to plain JSON, and migration gains a purge step covering both the settings file and the backup left by the previous migration.

## Impact

**Deleted**
- `Core/GARMIN/Client.Auth.cs` in full
- `Core/GARMIN/MagicStrings.cs` — the user agent and the CSRF and ticket regular expressions
- `Core/GARMIN/ClientFactory.cs` — consumer key retrieval
- `Core/GARMIN/Dto/GarminApiConsumerKeys.cs`, `Dto/Garmin/SendCredentialsResult.cs`, `Dto/GarminAuthenciationResult.cs`
- The sign-in, embed, multi-factor, OAuth1, and exchange entries in `Core/GARMIN/URLs.cs`
- The `OAuth.DotNetCore` dependency, whose only purpose was OAuth1 request signing

**New**
- Mobile sign-in client with a shared cookie container across the sign-in and code-verification steps
- Token exchange and refresh client with a client identifier fallback chain
- Encrypted token store with one implementation per operating system
- Session implementation satisfying the abstraction from `extract-platform-agnostic-core`
- A sign-in view and a second-factor code prompt

**Modified**
- `Core/GARMIN/Client.cs` — upload uses the bearer token, honours rate-limit delays, and never logs response bodies unredacted
- `Core/GARMIN/IClient.cs`
- `ViewModels/SettingsViewModel.cs` and `Views/SettingsPage.xaml` — the stored password field is replaced by a sign-in action and a connection status
- The typed settings object and its migration

**Users**
- The application can connect again.
- Accounts with two-step verification work for the first time, with the code entered in the application.
- Signing in means entering an email and password once, then roughly once a month.
- No password is stored anywhere, and any previously stored copy is removed.

**Downstream changes**
The README's security section becomes factually wrong the moment this ships, which `documentation-overhaul` corrects. The previously planned `webview-login-capture` change is cancelled: it existed only to avoid a manual ticket-copying step that this flow removes entirely.

**Prior art**
The flow is reconstructed from `ulfdalen/scalebridge-sync` (MIT) and, upstream of it, `cyberjunky/python-garminconnect`. Attribution belongs in the source file that implements it.
