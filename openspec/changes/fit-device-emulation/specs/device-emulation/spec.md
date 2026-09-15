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

### Requirement: Identity beyond manufacturer and product is left alone

The application SHALL change only the manufacturer and the product. Serial numbers SHALL be passed through as the recording device wrote them, and the application SHALL NOT ask the user for a device identifier.

#### Scenario: The serial number is the one the recording device wrote

- **WHEN** an emulated activity is produced
- **THEN** every serial number in it is identical to the one in the source file

#### Scenario: No device identifier is requested

- **WHEN** the user enables emulation
- **THEN** the application asks only which device to present, and nothing else

### Requirement: An unusable device selection degrades to no emulation

When emulation is enabled but the stored device selection names nothing the application knows, it SHALL upload the file unchanged, SHALL log the condition, and SHALL NOT fail the upload.

#### Scenario: Upload proceeds without emulation

- **WHEN** emulation is enabled and the stored device selection cannot be resolved
- **THEN** the activity is uploaded with its original content and the upload is not reported as a failure

#### Scenario: The condition is visible

- **WHEN** an upload proceeds without emulation for this reason
- **THEN** the log states that emulation was skipped and why

### Requirement: The emulated device identity is written consistently

When emulation applies, the application SHALL present the activity as recorded by the selected device: every statement of manufacturer and product in the file SHALL name Garmin and the selected model.

#### Scenario: The file identity states the selected device

- **WHEN** an emulated activity is produced
- **THEN** its file identification reports Garmin as the manufacturer and the selected model as the product

#### Scenario: Every device record agrees

- **WHEN** an emulated activity is produced
- **THEN** every device information record in it reports the same manufacturer and product as the file identification

#### Scenario: No creator record is invented

- **WHEN** an emulated activity is produced from a file that had no creator record
- **THEN** the produced file has none either

### Requirement: What identifies a sensor is preserved

The application SHALL preserve everything that identifies an individual sensor: its serial number, its device index, its device type and its source type. Only the manufacturer and product SHALL change.

#### Scenario: A sensor keeps its own serial number

- **WHEN** the source activity contains a power meter with its own serial number
- **THEN** that serial number is unchanged in the produced file

#### Scenario: A sensor keeps its type

- **WHEN** the source activity records a device as a power meter
- **THEN** the produced file still records it as a power meter, with the same device index

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
