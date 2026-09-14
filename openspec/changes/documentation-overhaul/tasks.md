## 1. Gather recorded outcomes

- [ ] 1.1 Confirm `cross-platform-packaging-ci` is merged and a release exists
- [ ] 1.2 Collect the final artefact names and how a user chooses between the two macOS builds
- [ ] 1.3 Collect the verified macOS first-launch steps and the macOS version they were verified against
- [ ] 1.4 Collect the checksum verification commands per operating system
- [ ] 1.5 Collect the emulation observations recorded by `fit-device-emulation`, including whether training load and recovery time actually appeared
- [ ] 1.6 Collect the notification answer for macOS
- [ ] 1.7 Collect the observed re-authentication cadence, as opposed to the advertised token lifetime
- [ ] 1.8 Collect the settings, credential, and log storage locations per operating system
- [ ] 1.9 Note which of these remain unanswered, so the text states uncertainty rather than guessing
- [ ] 1.10 Create branch `docs/readme-overhaul`

## 2. Structure

- [ ] 2.1 Draft the section order: what it does, what you need, install, set up, how it behaves, limitations, background
- [ ] 2.2 Confirm the structure before writing prose
- [ ] 2.3 Inventory what must be carried over from the current document, particularly the Wahoo companion and Dropbox setup steps
- [ ] 2.4 Check whether Dropbox is still the only export route from a Wahoo device, rather than carrying the old instructions forward unexamined

## 3. Overview and prerequisites

- [ ] 3.1 Write the opening: what the application does, for whom, on which operating systems
- [ ] 3.2 Remove the Windows-only framing from the title and the opening text
- [ ] 3.3 Write the prerequisites, stating that no .NET runtime installation is required
- [ ] 3.4 Move the author's original motivation to a background section near the end

## 4. Wahoo and Dropbox setup

- [ ] 4.1 Rewrite the Wahoo companion export instructions, verifying them against the current application
- [ ] 4.2 Rewrite the Dropbox client instructions for both operating systems
- [ ] 4.3 Note the default Dropbox folder location per operating system

## 5. Installation

- [ ] 5.1 Write the Windows installation section
- [ ] 5.2 Execute it on a real Windows machine from a downloaded artefact
- [ ] 5.3 Write the macOS installation section, including how to choose between the two builds
- [ ] 5.4 Write the first-launch explanation in the same place as the macOS download, stating that the refusal is expected
- [ ] 5.5 Give the approval steps in the order to try them, and name the verified macOS version
- [ ] 5.6 State plainly that the build carries no paid signing certificate and why
- [ ] 5.7 Execute the macOS section on a real machine, from an artefact downloaded so that it carries the quarantine attribute
- [ ] 5.8 Add checksum verification commands for each operating system and run them

## 6. Signing in

- [ ] 6.1 Write the sign-in section: entering credentials in the application, what happens next
- [ ] 6.2 Document the second-factor step and that the code is entered in the application
- [ ] 6.3 State how often signing in must be repeated, using the observed cadence from 1.7
- [ ] 6.4 Confirm nothing in this section tells the user to store a password in a settings field

## 7. Device emulation

- [ ] 7.1 Describe what the feature changes about the uploaded activity
- [ ] 7.2 State that a Unit ID from a device the user owns is required, and where to find it
- [ ] 7.3 State that the application validates the Unit ID's format only and cannot verify it belongs to the selected model
- [ ] 7.4 State that the service applies its own calculations and ignores performance values carried inside the file
- [ ] 7.5 State the dependency on the user's own device syncing and on physiological synchronisation being enabled
- [ ] 7.6 Base the claims on the 1.5 observations; where an outcome was not observed, say so rather than implying it
- [ ] 7.7 Review the finished section and confirm no sentence promises recovery time

## 8. Security

- [ ] 8.1 Remove the previous clear-text warning
- [ ] 8.2 Write what is stored: settings, session credentials, logs
- [ ] 8.3 Give the storage location for each, per operating system
- [ ] 8.4 State that the account password is never stored
- [ ] 8.5 State how session credentials are protected on each operating system
- [ ] 8.6 State what that protection does not defend against
- [ ] 8.7 Confirm the document still contains a security section rather than having dropped the subject

## 9. Limitations

- [ ] 9.1 State that Linux is not supported
- [ ] 9.2 State any macOS-specific limitation, including the notification answer from 1.6
- [ ] 9.3 State the periodic re-authentication requirement
- [ ] 9.4 State that the application depends on an interface the service does not publish, that it broke in March 2026, and what the project does when that happens

## 10. Screenshots

- [ ] 10.1 Install a released artefact on Windows and capture the main view showing the log and counters
- [ ] 10.2 Capture the settings view on Windows, including the emulation fields
- [ ] 10.3 Capture at least one view on macOS
- [ ] 10.4 Replace the two existing images and remove the obsolete files
- [ ] 10.5 Confirm the images match what a user of the released version sees

## 11. Final verification

- [ ] 11.1 Fix the star history link to the current format
- [ ] 11.2 Follow every link in the document and confirm it resolves
- [ ] 11.3 Execute every command in the document on the operating system it targets
- [ ] 11.4 Search the document for the previous clear-text warning and confirm it is absent
- [ ] 11.5 Search the document for any claim that emulation produces recovery time and confirm none remains
- [ ] 11.6 Confirm no instruction asks the user to install a .NET runtime
- [ ] 11.7 Have someone who has never used the application follow the document from the beginning through to an uploaded activity
- [ ] 11.8 Fix whatever that walkthrough exposed
