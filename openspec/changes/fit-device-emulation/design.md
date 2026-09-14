## Context

The pipeline built by `extract-platform-agnostic-core` reads an activity file into memory, passes the bytes through an ordered set of transformations, and uploads the result. That set has been empty since it was created. This change supplies its first member.

A FIT file is a header, a sequence of definition and data messages, and a checksum. Definition messages declare which fields a subsequent data message carries and in what size. That structure is what makes this harder than "find the bytes and overwrite them": the field this feature most needs to write — the serial number in the file identifier — is frequently absent from a Wahoo file's definition altogether, and a field that is not declared cannot be filled in place.

Wahoo files also routinely carry things that are not ours to touch: device information records for paired sensors such as heart rate straps and power meters, and sometimes developer data fields written by Wahoo's own applications.

## Goals / Non-Goals

**Goals:**
- Garmin Connect attributes the activity to the selected device.
- Everything the transformation does not target survives unchanged.
- A corrupt result is impossible to upload — it fails loudly instead.
- Adding a device to the catalogue is adding a row, not writing code.
- The user is told plainly what this does and does not achieve.

**Non-Goals:**
- Injecting training load, training effect, or recovery values into the file. Garmin ignores them and computes its own.
- Generating or guessing Unit IDs. The mapping to device models is proprietary; a fabricated value is not equivalent to a real one.
- Emulating a device the user does not own. The feature exists so that a user's own activities are attributed to their own device.
- Rewriting sensor attribution. Paired sensors stay as recorded.
- Supporting FIT files that are not activities.

## Decisions

### D1 — Decode and re-encode with the official software development kit, not surgical byte patching

Patching bytes in place is attractive because it cannot disturb anything it does not touch. It fails on the case that matters: if the source file's definition message for the file identifier does not declare a serial number field — and Wahoo files often do not — there is nowhere to write it. Adding a field means rewriting the definition, which means shifting every subsequent byte, which is a re-encode with extra steps and none of the safety.

So the file is decoded into messages, the targeted messages are modified, and the whole is re-encoded by the software development kit, which owns definition generation, header construction, and checksum calculation.

The cost of that choice is the risk of losing content the decoder does not understand, which D6 addresses directly and D9 verifies.

### D2 — Exactly three messages are targeted

**File identifier** — manufacturer set to Garmin, product set to the selected device's identifier, serial number set to the user's Unit ID. The creation timestamp and the file type are left alone: the timestamp is the activity's identity in Garmin Connect, and changing it would move the activity in time.

**File creator** — software and hardware version, taken from the catalogue entry. Added if the file does not already carry this message.

**Device information, the record describing the recording device itself** — manufacturer, product, serial number, and software version, made consistent with the file identifier.

Nothing else is touched: sessions, laps, records, events, heart rate variability, and the activity message are all passed through.

### D3 — Only the recording device's information record is rewritten

A Wahoo file typically contains several device information records: one for the head unit, and one for each paired sensor. Rewriting all of them would claim that the user's heart rate strap and power meter are also Garmin devices, which is false, breaks sensor attribution in Garmin Connect, and was never the point.

Only the record describing the recording device is modified. Sensor records are passed through untouched.

### D4 — The serial number is identical everywhere it appears

The file identifier and the recording device's information record must carry the same serial number. An inconsistency there is exactly the kind of thing a server-side validator notices, and it would produce a rejection whose message explains nothing.

One value, written in both places, from one source.

### D5 — Unit ID validation is format-only, and the interface says so

The Unit ID is a 32-bit number, and zero means "absent" in the FIT specification. Validation therefore checks that the value is numeric, in range, and not zero.

It cannot check anything else. Whether the Unit ID belongs to a device of the selected model is knowable only to Garmin, since the mapping is proprietary. The interface states this rather than implying a validated field: an accepted Unit ID means well-formed, not correct.

### D6 — Unknown and developer content is preserved, or the transformation fails

Decoders drop what they do not recognise unless told otherwise. A Wahoo file may carry developer data fields; silently discarding them would be data loss disguised as a feature.

The implementation preserves unrecognised messages and developer field definitions through the round trip. Where preservation cannot be guaranteed for a given file, the transformation fails and the activity is reported as failed — which retains the source file — rather than uploading a file with content quietly removed.

Losing data is worse than not applying the feature.

### D7 — The catalogue is data

A table of rows: display name, product identifier, plausible software and hardware version, and device kind. Adding the Edge 1060 when it exists is adding a row.

The initial rows are Fenix 7 (3906), Fenix 7S (3905), Fenix 7X (3907), Fenix 7 Pro Solar (4375), Fenix 8 (4536), Forerunner 965 (4315), Edge 1040 (3843), and Edge 1050 (4440). The identifiers come from the software development kit's own product enumeration, referenced by name in code rather than transcribed as literals, so a typo is a compilation error instead of a wrong device in Garmin Connect.

### D8 — Version values are cosmetic and treated as such

The software and hardware versions written into the file creator message have no known effect on Garmin's processing; they exist because a real device writes them. Each catalogue row carries a plausible value, and the design records that these are cosmetic so nobody later invests effort in tracking real firmware releases.

### D9 — The output is verified by decoding it again

After encoding, the produced bytes are decoded a second time in the same operation, and the result is checked: the targeted fields hold the intended values, and the message inventory matches the input except for the intended changes.

The files are small, so this costs nothing measurable. What it buys is the difference between a silently corrupt upload and a loud failure — and silent corruption is the characteristic failure mode of file rewriting.

### D10 — Non-activity files pass through untouched

A file that is a FIT file but not an activity — a course, a settings export, a workout — is passed through unchanged and the fact is logged. It is not an error, and it is not something to emulate a device for.

### D11 — Inconsistent settings degrade to disabled

If the feature is switched on but no valid Unit ID is stored, the transformation is skipped, the condition is logged, and the upload proceeds with the original bytes. The user's activities keep reaching Garmin Connect; only the attribution is missing.

Blocking uploads over a configuration problem would punish the user for the feature they opted into.

### D12 — The transformation is pure and works in memory

It takes bytes and returns bytes. It opens no file, writes no temporary file, and has no knowledge of where the activity came from. The source file on disk is therefore untouched by construction rather than by discipline, and the whole thing is testable with a byte array and no file system.

## Risks / Trade-offs

**The round trip drops content the decoder does not model** → This is the principal risk of D1. It is addressed three ways: preservation is implemented deliberately, D9 verifies the inventory on every single transformation rather than only in tests, and the test suite runs against real Wahoo exports rather than synthetic files. A file whose content cannot be preserved fails instead of uploading.

**A real Wahoo file turns out to have a structure the implementation did not anticipate** → Everything here is reasoned from the FIT specification and from what Wahoo files generally contain. The verification tasks require actual exports from the user's own device, and the round-trip test — decode and re-encode with no modification, then compare — is written before the patching logic, so structural surprises surface before they are entangled with the feature.

**Garmin stops honouring device identity, or tightens validation** → The feature degrades to cosmetic: activities still upload, they simply stop being credited the way the user hoped. Nothing breaks. This is why the interface states what depends on Garmin's processing.

**A user enters a Unit ID that is not theirs** → Not technically detectable, since format-only validation is all that is possible. The documentation states that this is for attributing your own activities to your own device, and the honest framing is the control, because there is no other one.

**Expectations exceed what the feature delivers** → The most likely disappointment is recovery time not appearing, because it is computed on the watch and needs physiological synchronisation and a device sync afterwards. Stating this in the interface, next to the setting, is part of the change rather than a documentation afterthought.

**Emulation changes what a duplicate looks like to Garmin** → The pipeline's own duplicate protection is keyed on the source file's content, which the transformation never alters, so re-offering the same file is still recognised locally. Garmin's own duplicate detection sees modified bytes and may or may not match; the pipeline already treats a duplicate report as a normal outcome either way.

## Migration Plan

Branch `feat/fit-device-emulation`.

1. Add the software development kit and write the round-trip test first: decode a real Wahoo export, re-encode it without modification, decode the result, and assert the message inventory and field values match. **This test passing is the precondition for everything else.**
2. Implement the catalogue as data, referencing the kit's product enumeration by name.
3. Implement the patch for the three targeted messages, including adding the file creator message when absent.
4. Implement the recording-device-only rule for device information records.
5. Implement output verification by re-decoding.
6. Implement the pass-through cases: non-activity files, feature disabled, settings inconsistent.
7. Add the settings: toggle, device selection, Unit ID, with format validation.
8. Add the settings page fields, the guidance on where to find the Unit ID, and the text stating what Garmin does and does not compute.
9. Register the transformation with the pipeline.
10. Verify end to end: upload an emulated activity and confirm in Garmin Connect that it is attributed to the selected device.

**Rollback:** revert the branch. Activities uploaded while the feature was active stay as they were uploaded; reverting changes nothing already in Garmin Connect. Settings gain three unknown values, which the settings store ignores.

## Open Questions

- Do real Wahoo exports carry developer data fields, and does the round trip preserve them? Answered by step 1 against actual files.
- Does the file creator message already exist in Wahoo exports, or must it always be added? Answered by step 1.
- Are there device information records in Wahoo files whose role is ambiguous — neither clearly the head unit nor clearly a sensor? If so, the safe reading is to leave them alone.
- Does Garmin Connect display the emulated device name from the product identifier alone, or does it also expect a product name field? Answered at step 10 by looking at the result.
- Does the activity actually contribute to training load after emulation, and does recovery time appear after a device sync? This is the question the whole feature exists for, and it can only be answered by observation over several days. It is recorded as an observation, not as an acceptance criterion, because it depends on Garmin's processing rather than on this code.
