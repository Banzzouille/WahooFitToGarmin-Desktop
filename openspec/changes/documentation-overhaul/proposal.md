## Why

By the end of the modernization sequence the README describes an application that no longer exists: a Windows-only tool, installed by unzipping an executable, authenticated with a Garmin login and password stored in clear text, illustrated by screenshots of a user interface that has been replaced. Every individual change corrects the facts it personally invalidates, but no change owns the document's structure, its screenshots, or its framing — and those are exactly what a first-time visitor judges the project by.

## What Changes

- Restructure the README around two supported operating systems. The current title and opening line describe the project as "a windows desktop version"; both are rewritten.
- Split installation into a Windows path and a macOS path, with the macOS path covering the unsigned bundle: why the operating system blocks it on first launch and the exact steps to allow it.
- Replace the login instructions. The Garmin login and password fields on the settings page no longer exist; authentication happens through an embedded login window, with a manual service-ticket paste as fallback. Both paths are documented, including what to do when the browser-based capture fails.
- Document device emulation: what selecting a Garmin model does to the uploaded file, why the Unit ID is required rather than optional, where to find it, and — stated plainly — what Garmin does and does not recompute on its side, so the feature is not oversold.
- **BREAKING** for readers of the current document: the section stating that all information is saved in clear text is removed, because it is no longer true once credentials are replaced by encrypted tokens. A rewritten section describes what is actually stored, where, and how it is protected on each operating system.
- Replace both screenshots. `Pictures/settings.png` and `Pictures/Example_interface.png` show the WPF interface; the settings page has also gained device selection and Unit ID fields. New screenshots are produced for both operating systems.
- Document the data, settings, and log file locations per operating system, replacing the single hard-coded Windows path.
- Document autostart for both operating systems, replacing the Windows-only Startup folder instruction.
- Correct the star history link, which uses the retired anchor format.
- Record the known limitations honestly: no Linux support, notifications that may not be delivered on an unsigned macOS bundle, and re-authentication roughly every thirty days.

Out of scope: the factual corrections each earlier change makes to the README as it goes. Those stay where they are, so that no intermediate merge leaves the document stating something false about the code that ships with it.

## Capabilities

### New Capabilities

- `user-documentation`: what the project's user-facing documentation must cover for a new user to install, configure, and operate the application on either supported operating system, and the accuracy guarantees that documentation must meet.

### Modified Capabilities

None. This change alters documentation only; no requirement about application behaviour changes.

## Impact

**Files**
- `README.md` — restructured
- `Pictures/settings.png`, `Pictures/Example_interface.png` — replaced
- New screenshots for the second operating system

**Dependencies between changes**
This change is sequenced last. It requires `avalonia-ui-port` for the interface shown in the screenshots, `fit-device-emulation` for the device selection fields, `garmin-di-oauth2-core` and `webview-login-capture` for the login procedure, and `cross-platform-packaging-ci` for the download artefacts and the macOS quarantine instructions.

**Users**
- A macOS user can install and run the application from the README alone.
- The obsolete clear-text warning is gone, replaced by an accurate description of what is stored.
- Expectations about recovery time and training load are set correctly before the user enables device emulation.

**No impact**
No source code, no dependency, no build configuration.
