## Why

Activities uploaded from a Wahoo device arrive in Garmin Connect as third-party data. Garmin does not compute training load, training status, or recovery time for activities that did not come from one of its own devices or a certified partner, so a user who rides with Wahoo hardware but tracks their training on Garmin Connect sees an incomplete picture. The application currently uploads the file's bytes untouched, so there is nothing in the pipeline that could change that.

Rewriting the device identity inside the file before upload is what makes Garmin treat the activity as its own — and it is the feature that motivated this entire modernization.

## What Changes

- Add an optional transformation that rewrites the device identity inside the activity file before upload: the manufacturer, the product, the serial number, and the creating software identity. The messages carrying that identity — the file identifier, the file creator, and the device information record — are made consistent with each other.
- Offer a short list of Garmin devices to emulate: Fenix 7, Fenix 7S, Fenix 7X, Fenix 7 Pro Solar, Fenix 8, Forerunner 965, Edge 1040, and Edge 1050.
- **Require the user's own device Unit ID** when the feature is enabled. The mapping between Unit ID ranges and device models is proprietary and cannot be generated, so a fabricated value is not equivalent to a real one. The settings page explains where to find it.
- Keep the feature **off by default**. An upgrading user's behaviour is unchanged until they opt in.
- Leave the file on disk untouched. Only the uploaded bytes are transformed.
- Produce a structurally valid file: every message the transformation does not target is preserved, the checksum is recomputed, and the result decodes cleanly.
- Refuse to upload rather than upload a corrupted file. If the transformation fails, the activity is reported as failed and the source file is retained.
- State the limits in the interface, not only in the documentation. Selecting a device does not make Garmin recompute everything: Garmin ignores training load values carried inside the file and applies its own algorithm, and recovery time is produced by the watch rather than by the server, which requires the account's physiological synchronisation to be enabled and the device to sync afterwards.

This change plugs into the transformation step that `extract-platform-agnostic-core` created for it. No pipeline restructuring is required.

## Capabilities

### New Capabilities

- `device-emulation`: which devices can be emulated, what the application writes into the file, the Unit ID requirement and its validation, the opt-in behaviour, and what the user is told about what Garmin will and will not compute.
- `fit-file-integrity`: the guarantees the transformation must preserve — a valid, decodable file, untargeted content unchanged, a correct checksum, and the source file on disk left alone.

### Modified Capabilities

None. `activity-watch-pipeline` already requires a transformation step and specifies that an empty set uploads the original bytes; this change supplies a member of that set without changing what the pipeline guarantees.

## Impact

**Core library — new**
- FIT decode, patch, and encode implementation satisfying the transformation abstraction
- The device catalogue: display name and product identifier per supported model

**Core library — modified**
- The typed settings object gains the emulation toggle, the selected device, and the Unit ID

**Desktop project — modified**
- The settings page gains the device selector, the Unit ID field with guidance on where to find it, and the explanatory text about what Garmin computes
- Validation preventing the feature from being enabled without a Unit ID

**Dependencies**
- Added: `Garmin.FIT.Sdk`, the official FIT software development kit. It targets .NET Standard 2.0, so it is consumable from the core library without a platform constraint, and it has no dependencies of its own.

**Users**
- Activities can be presented to Garmin Connect as coming from a chosen Garmin device.
- Doing so requires entering the Unit ID of a device the user owns.
- Nothing changes for users who leave the feature off.
- Emulated activities appear in Garmin Connect attributed to the selected device rather than to the Wahoo unit.

**Honesty constraint**
The feature must not be presented as guaranteeing recovery time. The application states what it changes in the file and what depends on Garmin's own processing, so that a user whose recovery time does not appear understands why rather than filing it as a defect.

**Downstream changes**
`avalonia-ui-port` ports the new settings fields. `documentation-overhaul` documents the feature, including its limits.
