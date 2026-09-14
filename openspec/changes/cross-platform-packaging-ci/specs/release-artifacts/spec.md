## ADDED Requirements

### Requirement: A release is produced from a tag

Publishing SHALL be triggered by a version tag and SHALL require no manual build steps. The version the application reports SHALL come from that tag.

#### Scenario: Tagging produces a release

- **WHEN** a version tag is pushed
- **THEN** artefacts are built, packaged, and attached to a published release without further manual action

#### Scenario: The reported version matches the tag

- **WHEN** an artefact built from a tag is launched
- **THEN** the version it displays matches the tag it was built from

#### Scenario: Releases do not depend on a particular machine

- **WHEN** a release is produced
- **THEN** it is built by the project's automation rather than on a contributor's computer

### Requirement: Artefacts are produced for Windows and both macOS architectures

Each release SHALL provide an artefact for Windows on x64, macOS on Apple Silicon, and macOS on Intel. Each artefact SHALL be clearly labelled so a user can tell which one applies to them.

#### Scenario: Three artefacts are attached

- **WHEN** a release is published
- **THEN** it carries one Windows artefact and two macOS artefacts, one per architecture

#### Scenario: Artefact names identify the platform and architecture

- **WHEN** a user views the release
- **THEN** each artefact's name states its operating system and processor architecture

### Requirement: No runtime prerequisite

Artefacts SHALL be self-contained, so that a user can run the application without installing a .NET runtime first.

#### Scenario: The application runs on a machine with no .NET installed

- **WHEN** an artefact is unpacked and launched on a machine with no .NET runtime present
- **THEN** the application starts

#### Scenario: Documentation states no runtime is needed

- **WHEN** the download instructions are read
- **THEN** they do not ask the user to install a runtime

### Requirement: The macOS artefact is a proper application bundle

The macOS artefacts SHALL be application bundles rather than bare executables, carrying the application's icon and its stable bundle identifier.

#### Scenario: The bundle behaves as a Mac application

- **WHEN** the macOS artefact is unpacked
- **THEN** it appears as a single application that can be moved to the Applications folder and launched from it

#### Scenario: The bundle identifier is the stable one

- **WHEN** the bundle metadata is inspected
- **THEN** it declares the reverse-DNS identifier the application uses for notifications

### Requirement: macOS artefacts carry a signature

Every macOS artefact SHALL be signed, at minimum ad-hoc, because an entirely unsigned binary will not execute on Apple Silicon. Signing SHALL be an explicit step rather than an assumed side effect.

#### Scenario: The artefact launches on Apple Silicon

- **WHEN** the Apple Silicon artefact is launched on an Apple Silicon machine, after the first-launch approval
- **THEN** it runs rather than being killed for having no signature

#### Scenario: Signing is explicit in the pipeline

- **WHEN** the release workflow is inspected
- **THEN** signing the macOS artefacts is a visible step

### Requirement: First-launch instructions are verified, not assumed

Because the artefacts are not signed with a paid developer identity, macOS will refuse the first launch. The project SHALL publish instructions that have been verified to work on a stated macOS version, and SHALL state which version they were verified against.

#### Scenario: Instructions accompany the download

- **WHEN** a user obtains a macOS artefact
- **THEN** the release notes and the README explain what the operating system will do and how to proceed

#### Scenario: The verified version is stated

- **WHEN** the first-launch instructions are read
- **THEN** they state the macOS version on which they were verified

#### Scenario: Following the instructions works

- **WHEN** a user follows the published steps on the stated macOS version
- **THEN** the application launches

#### Scenario: The tradeoff is stated rather than hidden

- **WHEN** a user reads why the operating system objects
- **THEN** the documentation states plainly that the build is not signed with a paid certificate and why

### Requirement: Downloads can be verified

Each release SHALL publish checksums for its artefacts, with the verification command documented for each operating system.

#### Scenario: Checksums are attached

- **WHEN** a release is published
- **THEN** a checksum file covering every artefact is attached to it

#### Scenario: Verification is documented

- **WHEN** a user wants to check what they downloaded
- **THEN** the command to do so is documented for their operating system

#### Scenario: A modified download fails verification

- **WHEN** an artefact is altered after publication
- **THEN** its checksum no longer matches the published value

### Requirement: Artefacts are launched before a release is considered good

The release process SHALL include launching each artefact on a real machine of its target platform, not merely producing it.

#### Scenario: Each artefact is launched

- **WHEN** a release candidate is prepared
- **THEN** each artefact is unpacked and launched on its target platform before the release is announced

#### Scenario: A pre-release is used for the first run of the process

- **WHEN** the release process runs for the first time
- **THEN** it targets a pre-release tag, so that a defective artefact is not published as a stable release
