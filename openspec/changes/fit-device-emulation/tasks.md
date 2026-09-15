## 1. Preconditions and sample material

- [x] 1.1 Confirm `extract-platform-agnostic-core` is merged, so the transformation step exists in the pipeline
- [x] 1.2 Create branch `feat/fit-device-emulation`
- [x] 1.3 Collect real Wahoo exports as test fixtures: at least one ride with paired sensors, one without, and one long enough to span multiple laps
- [ ] 1.4 Collect a FIT file that is not an activity, to exercise the pass-through path. Not supplied; the pass-through is covered by a test that builds a non-activity file rather than by a fixture
- [x] 1.5 Record the user's Garmin device model for the end-to-end verification — no Unit ID is needed, see design D5

## 2. Round-trip fidelity (gate)

- [x] 2.1 Add the FIT software development kit to the core library
- [x] 2.2 Implement decode of an activity into its messages
- [x] 2.3 Implement re-encode of those messages back to bytes
- [x] 2.4 Write the round-trip test first: decode a real Wahoo export, re-encode with no modification, decode the result, and compare message inventory and field values
- [x] 2.5 Verify that unrecognised messages survive the round trip
- [x] 2.6 Verify that developer-defined fields and their definitions survive the round trip
- [x] 2.7 Verify the round trip against every fixture collected in 1.3
- [x] 2.8 **STOP AND RE-PLAN if content is lost that cannot be preserved** — the whole approach depends on this test passing before any patching logic is written

## 3. Device catalogue

- [x] 3.1 Implement the catalogue as data: display name, product identifier, software version, hardware version, device kind
- [x] 3.2 Reference the software development kit's product enumeration by name; transcribe no numeric literal
- [x] 3.3 Populate the initial rows: Fenix 7, Fenix 7S, Fenix 7X, Fenix 7 Pro Solar, Fenix 8, Forerunner 965, Edge 1040, Edge 1050
- [x] 3.4 Record in a comment that the version values are cosmetic, so nobody later invests in tracking firmware releases
- [x] 3.5 Test: every catalogue row resolves to a product identifier

## 4. Identity patching

- [x] 4.1 Set manufacturer and product on the file identification message, leaving the serial number as recorded
- [x] 4.2 Preserve the file type, the creation timestamp and the serial number on that message
- [x] 4.3 Do not add a creator message; pass an existing one through unchanged
- [ ] 4.4 Confirm a source without a creator message still produces a file the service accepts
- [x] 4.5 Set manufacturer and product on every device information record
- [x] 4.6 Preserve each record's serial number, device index, device type and source type
- [x] 4.7 Confirm no serial number anywhere in the file differs from the source
- [x] 4.8 Test: manufacturer, product, and serial number are as configured
- [x] 4.9 Test: every serial number matches the source file
- [x] 4.10 Test: a sensor keeps its serial number, device index and device type
- [x] 4.11 Test: every device record reports the emulated manufacturer and product
- [x] 4.12 Test: creation time and every data timestamp are unchanged
- [ ] 4.13 Test: no performance or physiological value is added or modified
- [x] 4.14 Test: a file without a creator message does not gain one

## 5. Output verification

- [x] 5.1 Decode the produced bytes as part of every transformation, not only in tests
- [x] 5.2 Confirm the intended manufacturer, product, and serial number are present in the decoded output
- [x] 5.3 Confirm the message inventory matches the source, allowing for an added creator message
- [x] 5.4 Fail the transformation when verification does not hold
- [ ] 5.5 Test: a deliberately damaged output is rejected by verification
- [ ] 5.6 Test: verification failure prevents the bytes from being returned

## 6. Pipeline integration and failure behaviour

- [x] 6.1 Implement the transformation against the abstraction from `extract-platform-agnostic-core`, taking bytes and returning bytes
- [x] 6.2 Ensure the transformation opens no file and writes no temporary file
- [ ] 6.3 Register the transformation with the pipeline
- [x] 6.4 Skip transformation entirely when the feature is disabled
- [x] 6.5 Pass non-activity files through unchanged and log the reason, without counting a failure
- [x] 6.6 Skip transformation and log when the feature is enabled but the Unit ID is missing or invalid, uploading the original bytes
- [ ] 6.7 On transformation or verification failure, report the activity as failed, retain the source file, and do not fall back to uploading the original
- [x] 6.8 Test: disabled feature produces bytes identical to the file content
- [ ] 6.9 Test: a non-activity file passes through and the failure count is unchanged
- [ ] 6.10 Test: enabled with an invalid Unit ID uploads the original and does not fail
- [ ] 6.11 Test: a failed transformation counts a failure, retains the file, and uploads nothing
- [ ] 6.12 Test: the source file is byte-for-byte unchanged after both success and failure

## 7. Settings

- [ ] 7.1 Add the emulation toggle and the selected device to the typed settings, defaulting to disabled
- [ ] 7.2 Resolve the stored device selection against the catalogue, treating an unknown identifier as emulation disabled
- [ ] 7.3 Confirm an upgrading user's settings default to disabled
- [ ] 7.4 Test: a selected device is persisted and resolved on restart
- [ ] 7.5 Test: an unknown device identifier resolves to no emulation rather than throwing
- [ ] 7.6 Test: settings survive a restart
- [ ] 7.7 Test: a settings file from a previous version yields the feature disabled

## 8. User interface

- [ ] 8.1 Add the emulation toggle to the settings page
- [ ] 8.2 Add the device selector, populated from the catalogue
- [ ] 8.6 Add the text stating that exercise load is expected to appear, that recovery time is untested, and that both depend on the service's own processing
- [ ] 8.7 Review the wording so that nothing claims enabling emulation will produce recovery time
- [ ] 8.8 Verify the device selector and Unit ID persist across a restart

## 9. End-to-end verification

- [ ] 9.1 Upload a real Wahoo activity with emulation disabled and confirm nothing changed
- [ ] 9.2 Upload a real Wahoo activity with emulation enabled, using the user's own model and Unit ID
- [ ] 9.3 Confirm in Garmin Connect that the activity is attributed to the selected device
- [ ] 9.4 Confirm the activity's date, time, and data match the source ride
- [ ] 9.5 Confirm paired sensors still appear correctly attributed
- [ ] 9.6 Confirm the source file on disk is unchanged
- [ ] 9.7 Sync the user's Garmin device and observe, over the following days, whether training load and recovery time appear

## 10. Recording outcomes

- [ ] 10.1 Record whether Wahoo exports carry developer data fields and whether they survived
- [ ] 10.2 Record whether Wahoo exports already contain a creator message
- [ ] 10.3 Record whether any device information record was ambiguous, and that the safe reading was to leave it alone
- [ ] 10.4 Record whether the emulated device name displays correctly from the product identifier alone
- [ ] 10.5 Record the observed outcome of 9.7 as an observation, not as an acceptance criterion, and feed the wording of it into `documentation-overhaul`
- [ ] 10.6 Note for `avalonia-ui-port` that the device selector, Unit ID field, and explanatory text must be ported
