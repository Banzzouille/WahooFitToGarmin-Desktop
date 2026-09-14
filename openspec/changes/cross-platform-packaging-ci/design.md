## Context

The repository has no `.github` directory. Nothing is built, tested, or published automatically, and the current release is a hand-made zip of a framework-dependent Windows build whose README asks the user to install a .NET runtime first.

By the time this change runs, the application is an Avalonia desktop program targeting a platform-neutral framework, with a test project that runs anywhere. The work is therefore packaging and automation, not code — with one exception that turns out to matter: macOS refuses to run binaries that carry no signature at all, which makes "unsigned" a more nuanced position than it sounds.

## Goals / Non-Goals

**Goals:**
- Every change is built and tested on both operating systems before it can merge.
- A release is a tag, and produces the same artefacts whoever creates it.
- A user downloads one file, unpacks it, and runs it — no runtime to install first.
- A macOS user is told exactly what to do about the operating system refusing an unsigned application, before they hit it.
- A download can be verified.

**Non-Goals:**
- Paid code signing or notarisation on either platform.
- An installer, a package manager formula, or an auto-update mechanism.
- Linux artefacts. The application is untested there and shipping a build nobody has run is worse than shipping none.
- Windows on ARM as a separate artefact.
- Reproducible builds in the strict, bit-for-bit sense.

## Decisions

### D1 — Three artefacts: Windows x64, macOS Apple Silicon, macOS Intel

Windows on ARM runs x64 binaries through emulation, and the audience for a Wahoo-to-Garmin bridge running on a Windows ARM device is small enough that a fourth artefact is not worth the release surface. It can be added as a row later if someone asks.

macOS needs both architectures as separate artefacts rather than one universal binary. .NET does not produce universal binaries, and stitching two single-file bundles together with the platform's binary-merging tool does not work cleanly for self-contained applications. Two clearly labelled downloads, with the README explaining which is which, is the honest low-complexity answer.

### D2 — Self-contained, single-file, not trimmed

Framework-dependent builds are a fraction of the size, at the cost of a prerequisite. That prerequisite is the single most common source of "it does not start" in this class of application, and the current README already asks users to go and install a runtime before anything works. Removing that step is worth the size.

Trimming would claw some size back and is refused here: Avalonia resolves types through reflection, trimming breaks that in ways that appear at runtime rather than at build time, and a trimmed build that crashes on a page nobody tested is a bad trade for a download that happens once. Ahead-of-time compilation is refused for the same reason at this stage.

The result is a large download, and that is the accepted cost.

### D3 — macOS artefacts are ad-hoc signed, which is not the same as signed

Apple Silicon refuses to execute binaries with no signature whatsoever. "Unsigned" in this project's sense means "not signed with a paid Developer ID and not notarised" — it does not mean "carrying no signature", because that would simply not run.

Every macOS artefact is therefore ad-hoc signed, which costs nothing and requires no Apple account. Builds are produced on a macOS runner so the platform's signing tool is available, and signing is an explicit step rather than something assumed to happen by default.

This is also the mitigation identified by `avalonia-ui-port` for notification delivery. Whether it is sufficient is answered by that change before this one commits to unsigned distribution.

### D4 — The first-launch instructions must match what recent macOS actually does

The familiar advice — right-click the application and choose Open — reflects older macOS behaviour. Recent versions have tightened the path for quarantined applications without a Developer ID, pushing the user through system privacy settings instead, and there is also the command that removes the quarantine attribute directly.

Rather than publishing folklore, the instructions are verified against the macOS version the project actually targets, and the release notes carry whichever path works, in the order the user should try. Documentation that does not work is worse than none, because it makes the user believe the download is broken.

### D5 — Continuous integration runs the same checks on both operating systems

Build, test, and format verification, on Windows and macOS, for every push and pull request. The test project is platform-neutral, so both legs run the full suite.

Formatting is verified by tool rather than argued in review. This requires a formatting pass as part of this change — enabling the check against an unformatted repository produces a red build on day one and teaches everyone to ignore it.

### D6 — Releases are triggered by a tag and built on matching runners

A version tag starts the release workflow. The macOS artefacts are built on a macOS runner because signing requires it; the Windows artefact is built on a Windows runner for symmetry and to keep each leg's toolchain native.

The version comes from the tag and is stamped into the assembly, which is what the application displays — `modernize-dotnet10-foundation` already made version reporting independent of file paths precisely so this works under single-file publishing.

### D7 — Third-party actions are pinned by commit

A workflow that references an action by a moving tag executes whatever that tag points to today, with repository write access. Actions are pinned to a commit identifier, and the set of them is kept small.

### D8 — Checksums accompany every release

A checksum file is produced in the release job and attached alongside the artefacts, with the verification command documented per operating system. It is cheap, and for unsigned downloads it is the only integrity signal a user has.

### D9 — Autostart is a setting in the application, not a manual procedure

The current README tells Windows users to place a shortcut in a startup folder by hand. That instruction breaks with this change, and writing an equivalent manual procedure for macOS would be worse.

Autostart becomes a toggle in settings, with one implementation per operating system behind a single abstraction: a per-user registration on Windows, a launch agent on macOS. Enabling writes the registration with an absolute path; disabling removes it.

Registration must survive the application being moved or removed without leaving something that fails silently at every login, so the enable path is idempotent and the application tolerates finding a stale registration for a path that no longer exists.

### D10 — Dependency updates are proposed automatically

Both package dependencies and workflow actions are covered, on a monthly cadence. This project began this modernization with dependencies four years past their support window; the mechanism that prevents a repeat costs one configuration file.

### D11 — What the pipeline deliberately does not do

No signing beyond ad-hoc, no notarisation, no store submission, no auto-update, no publishing to any package manager. Each is a defensible future addition. None is required for releases to be reproducible, which is the problem this change exists to solve.

## Risks / Trade-offs

**Single-file publishing breaks something Avalonia needs at runtime** → Native assets and libraries that expect to find files on disk are the usual casualties, and the failure appears at launch rather than at build. Both artefacts are launched on a real machine as part of the release checklist, not merely produced.

**Apple Silicon refuses the build because signing was assumed rather than done** → D3 makes signing an explicit step. The check is launching the artefact on Apple Silicon, which the release checklist requires.

**The first-launch instructions are wrong for the user's macOS version** → D4 addresses it by verification rather than by copying advice. The risk remains for macOS versions the project has not tried, so the instructions state which version they were verified against.

**Notifications turn out not to work from an ad-hoc signed bundle** → Already designed around: `avalonia-ui-port` requires notification failure to be non-fatal, with the tray and the log as guaranteed channels. If the answer is negative, it becomes a documented macOS limitation and an argument to revisit paid signing — a decision with a price attached, taken with evidence rather than in advance.

**Artefact size** → Tens of megabytes per download, from removing the runtime prerequisite. Accepted, and stated in the release notes so nobody wonders why.

**GitHub-hosted runner images change under the project** → Runner images are updated by the provider, and a workflow that passes today can fail tomorrow for reasons unrelated to the code. Mitigation is a small workflow surface and pinned actions; the residual risk is accepted, since the alternative is self-hosted infrastructure nobody wants to maintain.

**Enabling format verification turns the build red immediately** → The formatting pass happens in this change, before the check is switched on.

## Migration Plan

Branch `ci/packaging`.

1. Confirm the notification answer from `avalonia-ui-port` before committing to unsigned distribution.
2. Run the formatting pass across the repository as its own commit.
3. Add the continuous integration workflow: build, test, format check, both operating systems. Confirm it is green before adding anything else.
4. Add publish settings to the user interface project: self-contained, single-file, the three runtime identifiers, no trimming.
5. Produce the macOS bundle layout, including the identifier fixed by `garmin-di-oauth2-core` and an application icon.
6. Add ad-hoc signing as an explicit step for both macOS artefacts.
7. Add the release workflow on tag: publish, package, checksum, create the release, attach artefacts.
8. Add dependency update configuration.
9. Implement autostart for both operating systems behind one abstraction, with the settings toggle.
10. Verify the macOS first-launch path on a real machine and record the macOS version it was verified against.
11. Cut a pre-release tag and run the full checklist: download each artefact, verify its checksum, launch it, sign in, process a file.
12. Update the README's download and first-launch sections, pending the fuller rewrite.

**Rollback:** delete the tag and the release. Workflows can be disabled without touching application code. The one irreversible element is a published release that users have already downloaded, which is why step 11 uses a pre-release tag first.

## Open Questions

- Does the single-file artefact launch correctly on each platform, or does something need to stay outside the bundle? Step 11.
- Which first-launch path actually works on current macOS, and on which version was that verified? Step 10.
- Does an ad-hoc signed bundle deliver notifications? Answered upstream, confirmed at step 11.
- Does the Windows autostart registration survive an in-place update to a new release, or must it be re-created each time? Worth knowing before the README claims either.
- Should a pre-release channel exist for testing tags, or is deleting a bad release sufficient? Leaning sufficient, given the project's size.
