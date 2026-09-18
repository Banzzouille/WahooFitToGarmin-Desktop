## 1. Preconditions

- [ ] 1.1 Confirm `avalonia-ui-port` is merged, so there is something to package for macOS
- [ ] 1.2 Retrieve the answer recorded by `avalonia-ui-port` on whether an ad-hoc signed bundle delivers notifications
- [ ] 1.3 **Decide explicitly whether unsigned distribution remains acceptable given that answer**, and record the decision
- [ ] 1.4 Confirm the bundle identifier fixed by `garmin-di-oauth2-core`
- [ ] 1.5 Create branch `ci/packaging`
- [ ] 1.6 Note the macOS version available for verification, since the first-launch instructions will be tied to it

## 2. Formatting pass

- [ ] 2.1 Run the formatter across the whole repository
- [ ] 2.2 Commit the result on its own, with no behavioural change mixed in
- [ ] 2.3 Confirm a subsequent format check reports no differences

## 3. Continuous integration workflow

- [x] 3.0 Pin restore to nuget.org with a repository `nuget.config`, so CI, the author, and contributors resolve packages from the same place regardless of machine configuration

- [x] 3.1 Add `.github/workflows/ci.yml` triggered on push and pull request
- [x] 3.2 Configure a matrix covering a Windows runner and a macOS runner
- [x] 3.3 Build the solution on both
- [x] 3.4 Run the tests on both, failing the workflow on any test failure
- [ ] 3.5 Add the format verification step
- [x] 3.6 Pin every third-party action to a commit identifier
- [x] 3.7 Green on both legs on GitHub
- [ ] 3.8 Verify that a deliberately broken build fails the correct leg
- [ ] 3.9 Verify that a deliberately unformatted file fails the format step
- [x] 3.10 `CONTRIBUTING.md` carries the three commands CI runs, verified to work as written

## 4. Publish configuration

- [x] 4.1 Add publish settings to the user interface project: self-contained, single file
- [x] 4.2 Declare the three runtime identifiers: Windows x64, macOS Apple Silicon, macOS Intel. All three are produced by the packaging workflow
- [x] 4.3 Confirm trimming and ahead-of-time compilation are disabled
- [ ] 4.4 Wire the version so it comes from the build rather than from a checked-in literal
- [x] 4.5 Publish each configuration locally and confirm it produces a runnable output
- [ ] 4.6 Launch the Windows output on a machine with no .NET runtime installed

## 5. macOS bundle

- [ ] 5.1 Produce the application bundle layout around the published executable
- [ ] 5.2 Write the bundle metadata, including the identifier from 1.4, the display name, and the version
- [ ] 5.3 Add the application icon in the platform's icon format
- [ ] 5.4 Add ad-hoc signing as an explicit, visible step for both macOS artefacts
- [ ] 5.5 Verify the Apple Silicon bundle launches on Apple Silicon rather than being killed for lacking a signature
- [ ] 5.6 Verify the Intel bundle launches
- [ ] 5.7 Verify the bundle can be moved to the Applications folder and launched from there

## 6. First-launch behaviour on macOS

- [ ] 6.1 Download the produced artefact the way a user would, so it carries the quarantine attribute
- [ ] 6.2 Observe exactly what the operating system does on first launch
- [ ] 6.3 Determine which approval path actually works on the macOS version noted in 1.6
- [ ] 6.4 Write the instructions in the order the user should try them
- [ ] 6.5 State in the instructions which macOS version they were verified against
- [ ] 6.6 State plainly that the build is not signed with a paid certificate, and why
- [ ] 6.7 Have someone follow the written instructions from scratch and confirm they work

## 7. Release workflow

- [x] 7.1 `.github/workflows/package.yml`, run manually rather than triggered by a tag, because releases are made by hand. Verified green, producing three artefacts
- [x] 7.2 Build the Windows artefact on a Windows runner
- [x] 7.3 Build, bundle, and sign the macOS artefacts on a macOS runner
- [x] 7.4 Stamp the version into the artefact names, read from the csproj rather than from a tag, since there is no tag trigger
- [x] 7.5 Name each artefact so its operating system and architecture are unambiguous
- [ ] 7.6 Generate a checksum file covering every artefact
- [~] 7.7 Not done on purpose: the release is created by hand, so the workflow uploads artefacts and keeps a read-only token
- [ ] 7.8 Include the macOS first-launch instructions in the release notes
- [ ] 7.9 Include a note that the download is large because no runtime install is required
- [x] 7.10 Pin every third-party action used here to a commit identifier

## 8. Dependency updates

- [ ] 8.1 Add `.github/dependabot.yml`
- [ ] 8.2 Cover package dependencies
- [ ] 8.3 Cover workflow actions
- [ ] 8.4 Set a monthly schedule
- [ ] 8.5 Confirm an update proposal is verified by the continuous integration workflow like any other change

## 9. Autostart

- [ ] 9.1 Define the autostart abstraction: query state, enable, disable
- [ ] 9.2 Implement per-user registration on Windows, requiring no elevation
- [ ] 9.3 Implement per-user registration on macOS, requiring no elevation
- [ ] 9.4 Make enabling idempotent, so enabling twice leaves one registration
- [ ] 9.5 Detect a registration pointing at a path that no longer exists, and repair or remove it
- [ ] 9.6 Make the settings toggle report the real registration state rather than the last requested value
- [ ] 9.7 Ensure disabling leaves no file or entry behind
- [ ] 9.8 Add the toggle to the settings page
- [ ] 9.9 Verify on Windows that the application starts after signing out and in again
- [ ] 9.10 Verify on macOS that the application starts after logging out and in again
- [ ] 9.11 Verify that moving the application is reflected in the toggle state
- [ ] 9.12 Verify that no administrative prompt appears

## 10. Pre-release rehearsal

- [ ] 10.1 Push a pre-release tag and let the release workflow run
- [ ] 10.2 Download each of the three artefacts as a user would
- [ ] 10.3 Verify each artefact against the published checksum
- [ ] 10.4 Launch the Windows artefact and complete a full cycle: sign in, detect a file, upload
- [ ] 10.5 Launch the Apple Silicon artefact, following the published first-launch steps, and complete the same cycle
- [ ] 10.6 Launch the Intel artefact and complete the same cycle, or record that no Intel machine was available for testing
- [ ] 10.7 Confirm the version displayed by each artefact matches the tag
- [ ] 10.8 Confirm notifications behave as the 1.2 answer predicted
- [ ] 10.9 Fix anything the rehearsal exposed and repeat before any stable tag

## 11. Documentation and handover

- [ ] 11.1 Update the README download section with the three artefacts and how to choose between them
- [ ] 11.2 Add the macOS first-launch steps and the verified macOS version
- [ ] 11.3 Add checksum verification commands for each operating system
- [ ] 11.4 State that the .NET runtime is no longer a prerequisite
- [ ] 11.5 State that a manually created Windows startup shortcut from an earlier version no longer works, and point to the autostart setting
- [ ] 11.6 Record for `documentation-overhaul` the final artefact names, the first-launch wording, and the verified macOS version
- [ ] 11.7 Record whether the Windows autostart registration survives an in-place update to a new release
- [ ] 11.8 Record whether a pre-release channel is worth keeping, or whether deleting a bad release is sufficient
