## ADDED Requirements

### Requirement: Device emulation is opt-in

The application SHALL NOT alter uploaded activity files unless the user has explicitly enabled device emulation. The feature SHALL be disabled by default, including for users upgrading from a version that did not have it.

#### Scenario: Disabled by default on a fresh installation

- **WHEN** the application is installed and started for the first time
- **THEN** device emulation is off and uploaded bytes are identical to the file's content

#### Scenario: Disabled by default after an upgrade

- **WHEN** a user upgrades from a version without this feature
- **THEN** device emulation is off and their uploads are unchanged

#### Scenario: Enabling takes effect on the next upload

- **WHEN** the user enables emulation and a new activity is detected
- **THEN** that activity is uploaded with the emulated device identity

#### Scenario: Disabling takes effect on the next upload

- **WHEN** the user disables emulation
- **THEN** subsequent uploads carry the file's original device identity

### Requirement: The user selects a device from a supported catalogue

The application SHALL offer a list of Garmin devices that can be emulated, each identified by a display name the user recognises. The catalogue SHALL be data rather than code branches, so that a device can be added without changing logic.

#### Scenario: The catalogue is offered for selection

- **WHEN** the user opens the emulation settings
- **THEN** the supported devices are listed by name, including Fenix 7 and Edge 1040

#### Scenario: The selected device is persisted

- **WHEN** the user selects a device and restarts the application
- **THEN** the same device is still selected

#### Scenario: Product identifiers come from the specification, not from transcription

- **WHEN** the catalogue implementation is inspected
- **THEN** each product identifier references the FIT specification's own product enumeration by name rather than a transcribed numeric literal

### Requirement: A Unit ID is required when emulation is enabled

The application SHALL require the user to supply their device's Unit ID before emulation can be applied. The application SHALL validate that the value is well-formed — numeric, within range, and not zero — and SHALL state that it cannot verify the value beyond its format.

#### Scenario: A well-formed Unit ID is accepted

- **WHEN** the user enters a numeric Unit ID within range
- **THEN** it is accepted and persisted

#### Scenario: A malformed Unit ID is rejected

- **WHEN** the user enters a non-numeric value, a value out of range, or zero
- **THEN** the application rejects it and explains what is expected

#### Scenario: The user is told where to find it

- **WHEN** the Unit ID field is displayed
- **THEN** guidance states where the value can be found on the user's device or account

#### Scenario: The limits of validation are stated

- **WHEN** a Unit ID is accepted
- **THEN** the interface conveys that the value is well-formed but cannot be checked against the selected device model

### Requirement: Emulation without a usable Unit ID degrades to no emulation

When emulation is enabled but no valid Unit ID is configured, the application SHALL upload the file unchanged, SHALL log the condition, and SHALL NOT fail the upload.

#### Scenario: Upload proceeds without emulation

- **WHEN** emulation is enabled and the stored Unit ID is missing or invalid
- **THEN** the activity is uploaded with its original content and the upload is not reported as a failure

#### Scenario: The condition is visible

- **WHEN** an upload proceeds without emulation for this reason
- **THEN** the log states that emulation was skipped and why

### Requirement: The emulated device identity is written consistently

When emulation applies, the application SHALL present the activity as recorded by the selected device: the manufacturer identifies Garmin, the product identifies the selected model, and the serial number carries the user's Unit ID. Every place in the file that states this identity SHALL agree.

#### Scenario: The file identity states the selected device

- **WHEN** an emulated activity is produced
- **THEN** its file identification reports Garmin as the manufacturer and the selected model as the product

#### Scenario: The serial number is the configured Unit ID

- **WHEN** an emulated activity is produced
- **THEN** the serial number it carries is the Unit ID the user configured

#### Scenario: Identity is consistent across the file

- **WHEN** an emulated activity is produced
- **THEN** the manufacturer, product, and serial number stated in the recording device's information match those in the file identification

#### Scenario: A creator record is present

- **WHEN** an emulated activity is produced from a file that had no creator record
- **THEN** the produced file contains one, consistent with the selected device

### Requirement: Sensor attribution is never rewritten

The application SHALL NOT alter device information describing paired sensors such as heart rate monitors or power meters. Only the record describing the recording device SHALL be modified.

#### Scenario: Paired sensors are untouched

- **WHEN** the source activity contains device information for a heart rate monitor and a power meter
- **THEN** those records are unchanged in the produced file

#### Scenario: Only the recording device changes

- **WHEN** the produced file's device records are compared with the source
- **THEN** exactly the record describing the recording device differs

### Requirement: Activity timing is never altered

The application SHALL NOT change the activity's creation time or any timestamp within the file. Emulation changes who recorded the activity, never when.

#### Scenario: Creation time is preserved

- **WHEN** an emulated activity is produced
- **THEN** its creation time is identical to the source file's

#### Scenario: Record timestamps are preserved

- **WHEN** an emulated activity is produced
- **THEN** every timestamp in the activity's data is identical to the source

### Requirement: Performance values are not fabricated

The application SHALL NOT inject training load, training effect, recovery time, or any other computed physiological value into the file.

#### Scenario: No computed values are written

- **WHEN** an emulated activity is produced
- **THEN** any performance summary values it contains are those the source file already carried

### Requirement: The user is told what emulation does not guarantee

The interface SHALL state, alongside the setting, that selecting a device does not by itself produce recovery time or training load. It SHALL convey that the service applies its own calculations and that some metrics depend on the user's device syncing afterwards.

#### Scenario: Limits are stated where the feature is enabled

- **WHEN** the user views the emulation settings
- **THEN** the interface states that recovery time and training load depend on the service's own processing and on the user's device syncing, not on this setting alone

#### Scenario: The feature is not described as a guarantee

- **WHEN** the wording of the emulation settings is reviewed
- **THEN** it does not claim that enabling emulation will produce recovery time
