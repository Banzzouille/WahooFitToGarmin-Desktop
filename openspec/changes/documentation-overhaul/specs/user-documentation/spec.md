## ADDED Requirements

### Requirement: A visitor learns what the software does before anything else

The documentation SHALL state what the application does, for whom, and on which operating systems, before describing its history, its author's motivation, or its setup.

#### Scenario: The purpose is stated first

- **WHEN** a visitor reads the first screen of the documentation
- **THEN** they learn what the application does and which operating systems it supports

#### Scenario: The project is not described as Windows-only

- **WHEN** the title and opening text are read
- **THEN** neither describes the project as a Windows application

#### Scenario: Background is present but not first

- **WHEN** the documentation is read in full
- **THEN** the motivation for the project is included, after the practical sections rather than before them

### Requirement: Installation is documented per operating system

The documentation SHALL provide separate installation instructions for Windows and for macOS, each stating which downloaded artefact applies and how to run it.

#### Scenario: A Windows user can install from the documentation alone

- **WHEN** a Windows user follows the Windows section
- **THEN** they obtain the correct artefact and reach a running application

#### Scenario: A macOS user can install from the documentation alone

- **WHEN** a macOS user follows the macOS section
- **THEN** they obtain the correct artefact for their processor and reach a running application

#### Scenario: Choosing between macOS artefacts is explained

- **WHEN** a macOS user views the download options
- **THEN** the documentation explains how to tell which processor architecture they need

#### Scenario: No runtime prerequisite is claimed

- **WHEN** the installation sections are read
- **THEN** they do not instruct the user to install a .NET runtime

### Requirement: The macOS first-launch refusal is explained before the user meets it

The documentation SHALL warn that macOS will refuse the first launch, give approval steps that have been verified to work, state the macOS version they were verified against, and explain why the build is not signed with a paid certificate.

#### Scenario: The refusal is anticipated

- **WHEN** a macOS user reads the installation section
- **THEN** they learn that the operating system will refuse the first launch, before they attempt it

#### Scenario: The approval steps work

- **WHEN** a macOS user follows the published approval steps on the stated macOS version
- **THEN** the application launches

#### Scenario: The verified version is stated

- **WHEN** the approval steps are read
- **THEN** they state which macOS version they were verified on

#### Scenario: The reason is given plainly

- **WHEN** the user asks why the operating system objects
- **THEN** the documentation states that the build carries no paid signing certificate and why that choice was made

#### Scenario: The warning sits where the user will meet it

- **WHEN** the documentation is laid out
- **THEN** the first-launch explanation appears with the macOS download instructions rather than in a separate section

### Requirement: Signing in is documented, including two-step verification

The documentation SHALL describe how the user connects the application to their account, including what happens when an account requires a second factor, and how often signing in must be repeated.

#### Scenario: The sign-in procedure is described

- **WHEN** a new user reads the setup instructions
- **THEN** they learn how to connect the application to their account

#### Scenario: Two-step verification is covered

- **WHEN** a user with two-step verification enabled reads the instructions
- **THEN** they learn that a code will be requested and how to supply it

#### Scenario: The repetition cadence is stated

- **WHEN** the sign-in section is read
- **THEN** it states that signing in must be repeated periodically, and roughly how often

#### Scenario: No stored password is implied

- **WHEN** the sign-in section is read
- **THEN** it does not instruct the user to enter a password into a settings field for storage

### Requirement: Device emulation is documented with its limits alongside its benefit

The documentation SHALL describe what device emulation changes, and SHALL state its constraints in the same section rather than elsewhere. It SHALL NOT claim that enabling the feature produces recovery time.

#### Scenario: The effect is described

- **WHEN** the emulation section is read
- **THEN** it states that the uploaded activity is presented as recorded by the selected device

#### Scenario: The Unit ID requirement is explained

- **WHEN** the emulation section is read
- **THEN** it states that a Unit ID from a device the user owns is required, where to find it, and that the application cannot verify it

#### Scenario: The service's own processing is stated

- **WHEN** the emulation section is read
- **THEN** it states that the service applies its own calculations and ignores performance values carried inside the file

#### Scenario: Recovery time is not promised

- **WHEN** the emulation section is reviewed
- **THEN** no sentence claims that enabling emulation will produce recovery time

#### Scenario: The dependency on device syncing is stated

- **WHEN** the emulation section is read
- **THEN** it states that some metrics require the user's own device to sync afterwards and the account's physiological synchronisation to be enabled

#### Scenario: Wording reflects observation

- **WHEN** the emulation section is written
- **THEN** its claims match what was observed during implementation, and where an outcome was not observed, the uncertainty is stated rather than assumed

### Requirement: The security section describes what is actually stored

The documentation SHALL state what the application stores, where it stores it on each operating system, and how it is protected. It SHALL NOT retain the previous claim that information is saved in clear text.

#### Scenario: The obsolete clear-text warning is gone

- **WHEN** the documentation is searched for the previous warning that all information is saved in clear text
- **THEN** it is absent

#### Scenario: A security statement is still present

- **WHEN** the documentation is read
- **THEN** it contains a section describing what is stored and how it is protected, rather than omitting the subject

#### Scenario: Storage locations are given per operating system

- **WHEN** a user wants to know where their data lives
- **THEN** the documentation gives the settings, credential, and log locations for their operating system

#### Scenario: No password storage is claimed or implied

- **WHEN** the security section is read
- **THEN** it states that the account password is not stored

#### Scenario: The protection is not overstated

- **WHEN** the security section describes how credentials are protected
- **THEN** it also states what that protection does not defend against

### Requirement: Known limitations are stated

The documentation SHALL include a section listing what the application does not do and what may stop working, so that a user meeting one of those situations recognises it rather than assuming a defect.

#### Scenario: Unsupported platforms are stated

- **WHEN** the limitations section is read
- **THEN** it states which operating systems are not supported

#### Scenario: The dependency on an unpublished interface is stated

- **WHEN** the limitations section is read
- **THEN** it states that the application depends on an interface the service does not publish, that this has broken before, and what the project does when it happens

#### Scenario: Platform-specific limitations are stated

- **WHEN** the limitations section is read
- **THEN** any behaviour that differs or may not work on a given operating system is named

### Requirement: Screenshots show the current application on both operating systems

The documentation SHALL illustrate the application with images of the interface as it currently is, taken from released artefacts, on both supported operating systems.

#### Scenario: Screenshots match the current interface

- **WHEN** a user compares the documentation's images with the running application
- **THEN** the interface shown is the one they see

#### Scenario: Both operating systems are represented

- **WHEN** the documentation's images are viewed
- **THEN** at least one shows the application running on each supported operating system

#### Scenario: The settings image shows the current fields

- **WHEN** the settings screenshot is viewed
- **THEN** it shows the fields the current version has, including device emulation

### Requirement: Every published instruction has been executed

Every command, path, and step in the documentation SHALL have been run on the operating system it targets before publication.

#### Scenario: Commands are verified, not recalled

- **WHEN** the documentation is prepared for publication
- **THEN** each command in it has been executed on its target operating system and produced the described result

#### Scenario: A newcomer can follow it end to end

- **WHEN** someone who has never used the application follows the documentation from the beginning
- **THEN** they reach a working installation that uploads an activity

#### Scenario: Links resolve

- **WHEN** the documentation's links are followed
- **THEN** each resolves to the intended destination, including the project statistics link
