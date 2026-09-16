## Context

`Client.Auth.cs` drives Garmin's **web** sign-in: prime cookies on the embed page, scrape a CSRF token out of HTML, POST a form, follow a redirect, pull a service ticket out of the response with a regular expression, sign an OAuth1 request, exchange for OAuth2. Garmin closed that path in March 2026 and the community library built on it was deprecated the same month.

Garmin's mobile application does not use that path. It posts JSON to a mobile sign-in endpoint and receives a service ticket in the response body, then exchanges the ticket for digital identity tokens. That flow still works, and a maintained implementation of it exists in `ulfdalen/scalebridge-sync` (MIT), which cites `cyberjunky/python-garminconnect` as its own upstream.

An earlier draft of this design assumed all programmatic sign-in was blocked and proposed having the user copy a service ticket out of browser developer tools. That was wrong, and this revision replaces it.

That correction cancelled the `webview-login-capture` change, on the reasoning that a web view existed only to automate the copying. D2 revisits that conclusion: the copying was never the only thing a web view was good for, and it is now the primary path rather than a cancelled one. The separate change stays cancelled — the web view lives here, beside the flow it feeds, rather than in a change of its own.

One uncertainty survives the correction and is addressed in D1.

## Goals / Non-Goals

**Goals:**
- The application authenticates and uploads again.
- Two-step verification works, including the methods a reimplementation cannot carry — passkeys, and a captcha if Garmin ever demands one.
- The user signs in about once a month.
- No password touches disk. On the primary path no password reaches the application at all; tokens are encrypted at rest.
- The authentication surface stays small and replaceable, because Garmin will change it again.

**Non-Goals:**
- A sign-in that requires the user to copy anything out of developer tools. This was the earlier draft and is what the web view removes, not what it reintroduces.
- Supporting Garmin endpoints beyond upload.
- Storing anything about the user beyond tokens and the identifier of the client that produced them.
- Registering an OAuth client with Garmin. There is no public registration, so there is no legitimate redirect URI and no officially sanctioned flow to implement. Both paths here are reconstructions of what Garmin's own applications do.

## Decisions

### D1 — Validate by hand first, specifically from the target environment

Everything rests on undocumented endpoints. Worse, the evidence conflicts: `peloton-to-garmin` reports that Cloudflare blocks the mobile sign-in endpoint outright, while `scalebridge-sync` uses that same endpoint and works.

The most plausible reconciliation is that the block is by client reputation rather than by endpoint — `peloton-to-garmin` is overwhelmingly deployed in containers on hosting providers, whose address ranges Cloudflare treats harshly, whereas a desktop application runs on a residential connection. If that is the explanation, this application is on the favourable side of the line, but it is a hypothesis, not a finding.

So validation runs **from the machine the application will run on**, not from a server:

**Step A — sign in**

```
curl -i -X POST \
  'https://sso.garmin.com/mobile/api/login?clientId=GCM_IOS_DARK&locale=en-US&service=https%3A%2F%2Fmobile.integration.garmin.com%2Fgcm%2Fios' \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -H 'Origin: https://sso.garmin.com' \
  -H 'User-Agent: Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148' \
  -c cookies.txt \
  -d '{"username":"EMAIL","password":"PASSWORD","rememberMe":true,"captchaToken":""}'
```

Record the status and which of the three shapes came back: a body containing `serviceTicketId`, a body whose `responseStatus.type` is `MFA_REQUIRED`, or a 429. **A 429 falsifies the hypothesis above and stops the change.**

**Step B — second factor, only if step A asked for one**

Reuse the cookie file; the code verification needs the session cookies that sign-in set.

```
curl -i -X POST \
  'https://sso.garmin.com/mobile/api/mfa/verifyCode?clientId=GCM_IOS_DARK&locale=en-US&service=https%3A%2F%2Fmobile.integration.garmin.com%2Fgcm%2Fios' \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -H 'Origin: https://sso.garmin.com' \
  -H 'Referer: https://sso.garmin.com/sso/signin?clientId=GCM_IOS_DARK&service=https%3A%2F%2Fmobile.integration.garmin.com%2Fgcm%2Fios' \
  -H 'User-Agent: <same as step A>' \
  -b cookies.txt -c cookies.txt \
  -d '{"mfaMethod":"METHOD","mfaVerificationCode":"CODE","rememberMyBrowser":true,"reconsentList":[],"mfaSetup":false}'
```

`METHOD` is the value `customerMfaInfo.mfaLastMethodUsed` returned in step A.

**Step C — exchange the ticket for tokens**

```
curl -i -X POST 'https://diauth.garmin.com/di-oauth2-service/oauth/token' \
  -H 'Authorization: Basic <base64 of "CLIENT_ID:">' \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -H 'User-Agent: <same as step A>' \
  --data-urlencode 'client_id=CLIENT_ID' \
  --data-urlencode 'service_ticket=ST-...-sso' \
  --data-urlencode 'grant_type=https://connectapi.garmin.com/di-oauth2-service/oauth/grant/service_ticket' \
  --data-urlencode 'service_url=https://mobile.integration.garmin.com/gcm/ios'
```

The base64 encodes the client identifier followed by a colon and nothing else — the colon is required. Try the identifiers in the order given in D3 and record which one is accepted, together with `expires_in` and the refresh token's lifetime. **Record values, never token strings.**

**Step D — refresh**

Same endpoint with `grant_type=refresh_token`, the same client identifier, and the refresh token. Record whether a *new* refresh token comes back.

**Step E — upload, the decisive step**

```
curl -i -X POST 'https://connectapi.garmin.com/upload-service/upload/.fit' \
  -H 'Authorization: Bearer <access token>' \
  -H 'User-Agent: <same as step A>' \
  -H 'NK: NT' \
  -F 'file=@activity.fit;type=application/octet-stream'
```

Record the status and whether the response still matches the existing `UploadResponse` model. If this fails, the change stops and is re-planned.

Steps A and B are run twice: once with two-step verification disabled, once with it temporarily enabled, since the account used for testing does not have it on today.

### D2 — Garmin's own page in an embedded web view, with the programmatic post as fallback

The user signs in on Garmin's real page, rendered in an embedded web view. The application watches for the redirect that carries the service ticket and takes it from there. It never sees the password.

The programmatic JSON post is kept, behind the same interface, as a fallback.

**Why the web view leads.** Not because two-step verification needs it — it does not. The mobile flow has a verification endpoint and the upstream implementation uses it, so codes by message, mail and authenticator application all work programmatically. The reason is the set of things a reimplementation cannot follow at all:

- A captcha. The sign-in payload carries an empty captcha field, which says the server side exists. If Garmin starts populating it, the programmatic path stops working outright, with nothing to be done about it.
- Passkeys, which Garmin offers and which cannot be driven from outside a browser.
- Whatever Garmin does to its sign-in next. On the page, that is Garmin's problem. In a reimplementation, it is ours, and it arrives as a support report rather than as a release note.

And the part no engineering argument captures: the user types their password into a Garmin page, not into a program they downloaded from a stranger. They can check the address bar. That is a different kind of assurance from a promise in a README that the password is not kept, and it is worth more than the code it costs.

**Why the programmatic path stays.** The web view is not free of failure modes of its own — on Windows it depends on a runtime that is usually but not always present, and that risk is recorded below. A path that works without any browser component is a genuine fallback rather than dead weight, and it is the path D1 validates, so it will exist and be understood regardless.

**Why this is cheap.** Both paths end at the same place: a service ticket. Everything after it — the exchange, the client identifier chain, refresh, storage, expiry reporting — is shared and untouched. The web view replaces step A and nothing else. Sign-in already sits behind a session abstraction so that Garmin can break it without the pipeline noticing; having two implementations of the same small step is what that abstraction was for.

**What it is not.** This is not OAuth as Garmin sanctions it. There is no public client registration and therefore no legitimate redirect to a loopback address. The web view renders Garmin's genuine page and the application intercepts the resulting navigation. That is better for the user than retyping a password into a form we wrote, and it is no more official.

### D3 — One consistent iOS persona, and a client identifier chain

Sign-in presents as Garmin's iOS application: client identifier `GCM_IOS_DARK`, service `https://mobile.integration.garmin.com/gcm/ios`, locale `en-US`, and an iPhone Safari user agent. The same user agent is used for the token exchange and for the upload. The current code sends an iOS user agent on upload already, so this is consistent with what the upload path does today — and consistency matters, because presenting as one client to obtain a token and as another to use it invites blocking.

The token exchange tries client identifiers in order, first acceptance winning:

```
GARMIN_CONNECT_MOBILE_ANDROID_DI_2025Q2
GARMIN_CONNECT_MOBILE_ANDROID_DI_2024Q4
GARMIN_CONNECT_MOBILE_ANDROID_DI
GARMIN_CONNECT_MOBILE_IOS_DI
```

Whichever succeeds is persisted with the tokens, because refresh must present the same one. Discovering it by cascade and then hard-coding a different one for refresh would fail a day later, far from its cause.

### D3a — The constants were re-read from upstream and had not moved

Checked against `ulfdalen/scalebridge-sync` on 16 September 2026, after the
design above was written from that same source. Unchanged: `GCM_IOS_DARK`, the
service URL, the iOS user agent, the empty `captchaToken` in the sign-in body,
and the four client identifiers in the order D3 gives — `2025Q2` is still the
head of the chain.

This is a freshness datum, not a new finding. It matters because every constant
here is undocumented and Garmin rotates the client identifiers by quarter: the
chain being unchanged means the design has not silently gone stale while other
changes were being built. It is worth re-reading again immediately before
implementation starts, and treating a moved head of the chain as a signal that
the quarter has turned rather than as a mistake.

Two implementation details from upstream that the decisions above imply but do
not spell out. The cookie container uses public-suffix matching rather than
plain domain matching, because sign-in and verification sit on hosts that must
share cookies correctly. And the expiry instant is persisted next to the token
pair and the accepted client identifier, which is what lets the interface state
whether the session is still good instead of discovering it on the next upload.

### D4 — Sign-in and code verification share a cookie container

Garmin's code verification step depends on session cookies set during sign-in. The two requests therefore share one cookie container, which lives for the duration of an authentication attempt and is discarded afterwards.

This is the single most likely implementation mistake: with a fresh HTTP client per request, sign-in succeeds, the code is accepted nowhere, and the error says nothing useful.

### D5 — `service_url` is a constant, never echoed from a response

The sign-in response contains a service URL. It is ignored. The token exchange sends the constant value.

Echoing back a value from a response into an authenticated exchange is how a compromised or manipulated sign-in response redirects a token somewhere it should not go. The upstream implementation carries an explicit comment to this effect, and the rule is worth restating rather than quietly following.

### D6 — Token endpoint response bodies never reach the log

Garmin echoes the submitted token inside the description of some `invalid_grant` errors. A well-meant "log the body so we can debug it" therefore writes a live credential into a file that users routinely paste into public issue trackers.

Two rules follow. Token endpoint failures are classified on the OAuth error code alone — `invalid_grant` means the refresh token is dead and the user must sign in again; anything else is reported by code and status. And any string shaped like a token is redacted before a body is written anywhere, as a second line of defence for the paths that do log bodies.

Passwords never appear in a log, an exception message, or a request that is retried after failure.

### D7 — Refresh tokens are single-use

The refresh response returns a new refresh token and invalidates the old one. An implementation that keeps using the original works exactly once and then locks the user out about a day later, with a symptom that looks nothing like its cause.

So: refresh is serialised, the returned pair is persisted **before** it is used, and only then is the refresh considered successful. Refresh runs a few minutes ahead of expiry rather than on failure, and concurrent triggers collapse into one in-flight attempt.

### D8 — Expired authentication pauses the queue instead of failing every activity

When the refresh token is dead, no retry can succeed. Letting each queued activity fail in turn produces a burst of failures, a misleading failure count, and files the user must re-trigger.

Instead the pipeline pauses: the current activity stays queued, processing stops, and the interface states that signing in again is required. Processing resumes automatically once sign-in succeeds.

This is why `upload-resilience` needs a delta — its current requirements treat renewal failure as an ordinary failure.

### D9 — Rate limiting is obeyed, not fought

An upload rejected with a rate-limit status carrying a retry delay waits for that delay rather than following the generic backoff. Authentication is never retried automatically after a rejection: wrong credentials are reported to the user, not attempted again on a timer, which is both useless and the behaviour that gets clients blocked.

### D10 — Tokens are protected by the operating system

An abstraction in the core library has one implementation per platform — the data protection API on Windows, the keychain on macOS — selected by runtime check rather than by target framework, so the core library stays platform-neutral as its own specification requires.

*Alternative considered:* a plain file with owner-only permissions, which is what the reference implementation does, arguing that encrypting a file with a key stored beside it protects nothing. The argument is sound but does not apply here: with the platform facility, the key is held by the operating system and tied to the user's login session, not stored beside the data. Owner-only permissions remain the floor, not the ceiling.

When the protected store cannot be read — a different Windows profile, a denied keychain prompt — the situation degrades to "sign in again", not to a crash.

The macOS implementation is written here but can only be exercised on real hardware during `avalonia-ui-port`'s macOS pass. Until then it is covered by tests against the abstraction, not by verified keychain behaviour.

### D11 — Purging credentials includes the backup file

`extract-platform-agnostic-core` migrates settings and deliberately keeps the previous file under a backup name. That file contains the Garmin password in clear text.

Removing the password from the live settings while leaving the backup on disk would be a purge in name only. Both are covered.

## Risks / Trade-offs

**The mobile sign-in endpoint is blocked from the user's network too** → This is the hypothesis D1 exists to test, and it is tested from a real user machine before any code is written. If step A returns 429 from a residential connection, the premise is dead and the change is re-planned — at which point the cancelled web-view approach returns as the candidate, which is why its cancellation is recorded with a reason rather than silently dropped.

**Garmin changes or closes this path as well** → It has happened once already, in March 2026. The authentication surface is deliberately small and sits behind the session abstraction, so it can be replaced without touching the pipeline. Existing tokens keep working for their remaining lifetime, so a break is gradual rather than instantaneous for every user. This risk cannot be engineered away: the application depends on an interface Garmin does not publish and owes nobody.

**Garmin starts requiring a captcha** → This is answered by D2 rather than deferred. On the web view path the challenge renders and the user solves it, as on any other site. The programmatic fallback has no answer and would simply stop working, which is one of the reasons it is the fallback and not the primary path.

**The Windows web view runtime is missing** → The web view uses WebView2 on Windows, which ships with Windows 11 and is present on most Windows 10 machines, but is not guaranteed. This is a real dent in the "unzip and run, nothing to install" property the packaging work was built around. It must be detected and reported as a clear instruction with a link, never as a crash or an empty window, and the programmatic path is offered when it is absent. Verified on a machine without the runtime before release, not assumed.

**A password leaks through a log or an exception** → D6 states the rule; it is verified by inspection rather than assumed, because this is the failure that matters most and the easiest to introduce by accident.

**Single-use refresh tokens mishandled** → D7 is written from the reference implementation's explicit warning rather than from guesswork, and step D confirms the behaviour before implementation.

**The keychain implementation is unverified until the macOS pass** → Stated plainly. If the keychain proves unusable, the fallback is signing in again at each launch on macOS — bad, not broken, and discovered by a task that exists rather than by a user.

**Reverse-engineered access may conflict with Garmin's terms of service** → The application acts for the account owner, with their own credentials, uploading their own activities. It is documented in the README so users can judge for themselves.

## Migration Plan

Branch `feat/garmin-mobile-auth`.

1. Run the D1 validation from a residential connection, including the two-step verification pass. **Stop if step A returns 429 or step E fails.**
2. Sign-in client: JSON request, shared cookie container, three response shapes — ticket, code required, rejected credentials.
3. Code verification step on the same cookie container.
4. Token exchange with the client identifier chain, refresh with the persisted identifier, single-use handling.
5. Error classification and token redaction, with tests asserting that no token-shaped string survives into a log.
6. Secret store abstraction with both platform implementations.
7. Session implementation satisfying the abstraction from `extract-platform-agnostic-core`, with proactive single-flight refresh.
8. Pipeline pause and resume on sign-in required; rate-limit delay honoured on upload.
9. Sign-in view and code prompt in the current user interface; remove the stored password field.
10. Purge: strip the password from settings, delete the legacy backup, drop the base64 wrapper.
11. Delete the old authentication code, the consumer key download, and the OAuth1 dependency.
12. End-to-end verification: first sign-in, upload, forced refresh, expiry, sign-in again — with two-step verification both off and temporarily on.

**Rollback:** revert the branch. Reverting restores code that cannot authenticate, so rollback is only meaningful before step 12; after that the choice is forward-fix, not revert. Purged passwords are not restored by a revert. This is stated so nobody treats revert as a safety net once the change has shipped.

## Open Questions

- Does the mobile sign-in endpoint answer from a residential connection, or is the block broader than the hosting-provider hypothesis? Step A, before anything else.
- Does the digital identity access token work for uploads on its own, or is a second token exchange required? Step E.
- Which client identifier is currently accepted, and how long do the two tokens actually last? Steps C and D.
- Does the code-verification step behave the same for an authenticator application as for an emailed code? Step B, with two-step verification temporarily enabled.
- Does asking Garmin to remember the session measurably reduce how often a code is demanded? Observable only over weeks; recorded as an observation, not a gate.
