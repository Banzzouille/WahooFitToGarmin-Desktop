## Why

The project has no automated build, no tests running anywhere but a developer's machine, and no release process. Its current distribution model is a zip file containing a framework-dependent Windows executable, produced by hand. Once the application runs on macOS, "produced by hand" stops scaling: two operating systems and three processor architectures cannot be built, checked, and published manually without something being forgotten.

This change makes the build reproducible, makes every change verified on both operating systems before merge, and turns a release into a tag.

## What Changes

- Add a continuous integration workflow that builds and tests on Windows and macOS for every push and pull request, so a change that compiles only on the author's machine cannot merge.
- Enforce formatting in the same workflow, so style is settled by a tool rather than in review.
- Add a release workflow triggered by a version tag, publishing for Windows x64, macOS Apple Silicon, and macOS Intel.
- Publish self-contained, single-file builds. **BREAKING** for existing users: the .NET Desktop Runtime is no longer a prerequisite, and the previous installation's files are not replaced in place — the new artefact is a different shape.
- Produce a macOS application bundle rather than a bare executable, so the application appears and behaves as a normal Mac application, with a stable bundle identifier for notifications.
- **Ship macOS unsigned.** A signing certificate is an annual cost this project does not carry. The release notes and the README state the tradeoff plainly and give the exact steps to run an unsigned application, rather than leaving the user with an unexplained refusal from the operating system.
- Publish checksums with every release so a download can be verified.
- Create a GitHub release on tag, with the artefacts attached and notes generated from the change history.
- Add automated dependency update proposals, which matters for a project whose packages were four years out of date at the start of this modernization.
- Provide autostart per operating system: a startup entry on Windows, a launch agent on macOS, together with the means to remove it. **BREAKING**: the manual shortcut described in the current README stops working, because the executable's name and layout change.

Deliberately not included: an installer, a package manager formula, an automatic update mechanism, and code signing on either platform. Each is a reasonable future addition and none is required to make releases reproducible.

## Capabilities

### New Capabilities

- `build-verification`: what must pass before a change can merge, on which operating systems, and what a contributor can run locally to get the same answer.
- `release-artifacts`: what a release produces for each platform, how a user verifies what they downloaded, and what they must do to run an unsigned application on macOS.
- `autostart`: how the application is registered to start with the user's session on each operating system, and how that registration is removed.

### Modified Capabilities

None. This change adds packaging and automation around the application without altering what the application does.

## Impact

**New files**
- `.github/workflows/ci.yml` — build, test, and format check on Windows and macOS
- `.github/workflows/release.yml` — publish, package, and create the release on tag
- `.github/dependabot.yml` — dependency update proposals
- macOS bundle metadata, including the bundle identifier fixed by `garmin-di-oauth2-core`
- Autostart registration and removal for both operating systems

**Modified**
- The user interface project file gains publish settings: self-contained, single-file, runtime identifiers
- `README.md` gains download instructions per platform and the unsigned-application steps, pending the full rewrite in `documentation-overhaul`

**Users**
- macOS builds become downloadable.
- No runtime prerequisite to install.
- On macOS, the first launch requires an explicit step to approve an unsigned application.
- An existing Windows autostart shortcut must be recreated.
- Downloads can be verified against published checksums.

**Project**
- Every pull request is verified on both operating systems.
- Releases stop depending on one person's machine being set up correctly.

**Dependencies between changes**
Requires `avalonia-ui-port`, since there is nothing to package for macOS before it. Feeds `documentation-overhaul`, which documents installation per platform and the unsigned-application steps in their final form. The question of whether an unsigned bundle can deliver notifications, raised by `avalonia-ui-port`, is answered before this change decides that unsigned is acceptable.
