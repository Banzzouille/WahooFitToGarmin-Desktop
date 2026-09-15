## Why

Activities uploaded from a Wahoo device arrive in Garmin Connect as third-party data. Garmin does not compute training load, training status, or recovery time for activities that did not come from one of its own devices or a certified partner, so a user who rides with Wahoo hardware but tracks their training on Garmin Connect sees an incomplete picture. The application currently uploads the file's bytes untouched, so there is nothing in the pipeline that could change that.

Rewriting the device identity inside the file before upload is what makes Garmin treat the activity as its own — and it is the feature that motivated this entire modernization.

## What Changes

- Add an optional transformation that rewrites the device identity inside the activity file before upload: the manufacturer and the product, in the file identifier and in every device information record.
- Offer a short list of Garmin devices to emulate: Fenix 7, Fenix 7S, Fenix 7X, Fenix 7 Pro Solar, Fenix 8, Forerunner 965, Edge 1040, and Edge 1050.
- **Ask for nothing but the device.** An earlier version of this proposal required the user to supply their Garmin device's Unit ID, on the strength of research saying the service checks the serial number against the model. A real conversion that produced an exercise load kept the Wahoo unit's own serial number, so the requirement was not real. Serial numbers are passed through untouched and there is no identifier to enter.
- Keep the feature **off by default**. An upgrading user's behaviour is unchanged until they opt in.
- Leave the file on disk untouched. Only the uploaded bytes are transformed.
- Produce a structurally valid file: every message the transformation does not target is preserved, the checksum is recomputed, and the result decodes cleanly.
- Refuse to upload rather than upload a corrupted file. If the transformation fails, the activity is reported as failed and the source file is retained.
- State the limits in the interface, not only in the documentation. Exercise load is confirmed to appear, and the emulated model is shown as the recording device — both observed in Garmin Connect on a converted file. Recovery time was not observed, on a file three years old, and recovery time is a forward-looking figure the watch computes from recent training, so an old activity could not have produced one. Load is demonstrated; recovery time is untested, and the interface says so rather than implying both.

This change plugs into the transformation step that `extract-platform-agnostic-core` created for it. No pipeline restructuring is required.

## Capabilities

### New Capabilities

- `device-emulation`: which devices can be emulated, what the application writes into the file and what it leaves alone, the opt-in behaviour, and what the user is told about what the service will and will not compute.
- `fit-file-integrity`: the guarantees the transformation must preserve — a valid, decodable file, untargeted content unchanged, a correct checksum, and the source file on disk left alone.

### Modified Capabilities

None. `activity-watch-pipeline` already requires a transformation step and specifies that an empty set uploads the original bytes; this change supplies a member of that set without changing what the pipeline guarantees.

## Impact

**Core library — new**
- FIT decode, patch, and encode implementation satisfying the transformation abstraction
- The device catalogue: display name and product identifier per supported model

**Core library — modified**
- The typed settings object gains the emulation toggle and the selected device

**Desktop project — modified**
- The settings page gains the device selector and the explanatory text about what the service computes

**Dependencies**
- Added: `Garmin.FIT.Sdk`, the official FIT software development kit. It targets .NET Standard 2.0, so it is consumable from the core library without a platform constraint, and it has no dependencies of its own.

**Users**
- Activities can be presented to Garmin Connect as coming from a chosen Garmin device.
- Doing so requires choosing a device from a list, and nothing else.
- Nothing changes for users who leave the feature off.
- Emulated activities appear in Garmin Connect attributed to the selected device rather than to the Wahoo unit.

**Honesty constraint**
The feature must not be presented as guaranteeing recovery time. Exercise load is
demonstrated; recovery time is not. The application states what it changes in the
file and what depends on the service's own processing, so that a user whose
recovery time does not appear understands why rather than filing it as a defect.

**Downstream changes**
`avalonia-ui-port` ports the new settings fields. `documentation-overhaul` documents the feature, including its limits.
