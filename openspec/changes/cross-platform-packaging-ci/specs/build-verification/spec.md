## ADDED Requirements

### Requirement: Every change is verified on both supported operating systems

The project SHALL build and run its automated tests on Windows and on macOS for every push and every pull request. A change that fails on either operating system SHALL be visible as a failure before it is merged.

#### Scenario: A pull request is verified on both platforms

- **WHEN** a pull request is opened
- **THEN** the solution is built and the tests are run on Windows and on macOS, and both results are reported

#### Scenario: A change that only compiles on one platform is caught

- **WHEN** a change compiles on Windows but not on macOS
- **THEN** the macOS leg reports a failure

#### Scenario: A failing test fails the workflow

- **WHEN** any test fails on either operating system
- **THEN** the workflow reports a failure rather than reporting success with a warning

#### Scenario: Verification runs on push as well as on pull request

- **WHEN** a commit is pushed to the main branch
- **THEN** the same verification runs

### Requirement: Formatting is enforced by tool

The project SHALL verify code formatting as part of the same workflow, so that formatting is settled mechanically rather than in review. The repository SHALL be formatted before the check is enabled.

#### Scenario: Unformatted code fails verification

- **WHEN** a change introduces code that does not match the project's formatting
- **THEN** the workflow reports a failure identifying what differs

#### Scenario: The check is green on the commit that introduces it

- **WHEN** the formatting check is enabled
- **THEN** the repository already satisfies it, so the first run passes

### Requirement: A contributor can reproduce the verification locally

The checks run by the workflow SHALL be runnable locally with documented commands, so that a contributor can get the same answer before pushing.

#### Scenario: Documented commands match the workflow

- **WHEN** a contributor runs the documented build, test, and format commands locally
- **THEN** they exercise the same checks the workflow runs

#### Scenario: Commands are discoverable

- **WHEN** a contributor looks for how to verify their change
- **THEN** the commands are documented in the repository

### Requirement: Third-party workflow actions are pinned

Workflows SHALL reference third-party actions by an immutable commit identifier rather than by a moving tag, and SHALL use as few of them as practical.

#### Scenario: No action is referenced by a moving tag

- **WHEN** the workflow files are inspected
- **THEN** every third-party action is pinned to a commit identifier

#### Scenario: Pinning is visible for review

- **WHEN** an action is updated
- **THEN** the change to the pinned identifier appears in the diff

### Requirement: Dependency updates are proposed automatically

The project SHALL receive automated update proposals for both package dependencies and workflow actions, on a regular schedule.

#### Scenario: Outdated packages produce a proposal

- **WHEN** a referenced package has a newer version
- **THEN** an update proposal is raised

#### Scenario: Workflow actions are covered too

- **WHEN** a pinned action has a newer commit
- **THEN** an update proposal is raised for it as well

#### Scenario: Proposals are verified like any other change

- **WHEN** an update proposal is opened
- **THEN** it is built and tested on both operating systems before it can be merged
