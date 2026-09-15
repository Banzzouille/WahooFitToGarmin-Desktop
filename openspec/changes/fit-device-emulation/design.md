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
- Inventing identity beyond the manufacturer and product. Serial numbers, timestamps, device indexes and sensor records are passed through as recorded.
- Emulating a device the user does not own. The feature exists so that a user's own activities are attributed to their own device.
- Rewriting sensor attribution. Paired sensors stay as recorded.
- Supporting FIT files that are not activities.

## Decisions

### D1 — Decode and re-encode with the official software development kit, not surgical byte patching

Patching bytes in place is attractive because it cannot disturb anything it does not touch. It fails on the case that matters: if the source file's definition message for the file identifier does not declare a serial number field — and Wahoo files often do not — there is nowhere to write it. Adding a field means rewriting the definition, which means shifting every subsequent byte, which is a re-encode with extra steps and none of the safety.

So the file is decoded into messages, the targeted messages are modified, and the whole is re-encoded by the software development kit, which owns definition generation, header construction, and checksum calculation.

The cost of that choice is the risk of losing content the decoder does not understand, which D6 addresses directly and D9 verifies.

### D2 — Exactly three messages are targeted

This was written before any real file had been examined, and two of its three
parts were wrong. What follows is what the evidence supports.

**File identifier** — manufacturer set to Garmin and product set to the selected
device's identifier. The serial number, the creation timestamp and the file type
are left alone. The timestamp is the activity's identity in the service and
changing it would move the activity in time; the serial number is discussed in
D5, where the original decision is reversed.

**Device information, every record** — manufacturer and product set to the same
values as the file identifier. Serial numbers, device indexes, device types and
source types are left untouched. See D3, where that decision is also reversed.

**File creator** — not added. The working conversion carried no such message and
the service accepted the file, so adding one would be inventing a requirement.
Where the source already has one it is passed through unchanged.

Nothing else is touched: sessions, laps, records, events, heart rate variability,
and the activity message are all passed through.

### D3 — Every device information record is rewritten, reversing an earlier decision

This decision previously said the opposite: only the recording device would be
touched, because claiming that a user's heart rate strap and power meter are
Garmin devices is false and would break sensor attribution.

The reasoning was sound and the conclusion was wrong. A conversion of a real ride
that the service accepted — and that produced an exercise load, confirmed by the
file's owner in Garmin Connect — rewrote manufacturer and product on all eighty
four device records, the power meter among them. Its serial numbers, indexes and
device types were left alone.

Reproducing a configuration known to work beats improving on one that has never
been tried. The narrower approach may well work too; nobody has demonstrated it,
and this feature is not the place to find out at a user's expense.

What stays untouched inside those records still matters: serial numbers, device
indexes, device types and source types are preserved, so the file continues to
say that a power meter was present and which one, by its own serial.

### D4 — Serial numbers are not touched, so they stay consistent by construction

The original decision was about writing one Unit ID into both the file identifier
and the device record without them drifting apart. With D5 reversed there is
nothing to write: every serial number in the file is the one the recording device
put there, and they remain as consistent with each other as they were.

Which is the stronger position anyway. The previous plan would have made the file
identifier claim one serial while eighty four device records claimed another,
unless every one of them were rewritten too.

### D5 — No Unit ID. The original serial number is kept

This reverses the most demanding part of the original design, and the feature is
much better for it.

The plan required the user to find and enter their Garmin device's Unit ID, on
the strength of research saying the service checks the serial number against the
device model. The interface would have had a mandatory field, format-only
validation, and a paragraph explaining where to find the value and why the
application could not verify it.

The working conversion kept the Wahoo unit's own serial number — 2519185680 —
and the service still produced an exercise load. The requirement was not real.

So the serial number is passed through untouched. There is no Unit ID field, no
validation, no guidance to write, and nothing for a user to get wrong. The
feature reduces to choosing a device from a list.

This also removes the one part of the design that was uncomfortable: asking
someone to type an identifier belonging to hardware they own, into a file
claiming to be from that hardware.

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

If the feature is switched on but the stored device identifier names nothing in
the catalogue — an entry removed by a later version, for instance — the
transformation is skipped, the condition is logged, and the upload proceeds with
the original bytes. The user's activities keep reaching the service; only the
attribution is missing.

Blocking uploads over a configuration problem would punish the user for the
feature they opted into.

### D12 — The transformation is pure and works in memory

It takes bytes and returns bytes. It opens no file, writes no temporary file, and has no knowledge of where the activity came from. The source file on disk is therefore untouched by construction rather than by discipline, and the whole thing is testable with a byte array and no file system.

## Risks / Trade-offs

**The round trip drops content the decoder does not model** → This is the principal risk of D1. It is addressed three ways: preservation is implemented deliberately, D9 verifies the inventory on every single transformation rather than only in tests, and the test suite runs against real Wahoo exports rather than synthetic files. A file whose content cannot be preserved fails instead of uploading.

**A real Wahoo file turns out to have a structure the implementation did not anticipate** → Everything here is reasoned from the FIT specification and from what Wahoo files generally contain. The verification tasks require actual exports from the user's own device, and the round-trip test — decode and re-encode with no modification, then compare — is written before the patching logic, so structural surprises surface before they are entangled with the feature.

**Garmin stops honouring device identity, or tightens validation** → The feature degrades to cosmetic: activities still upload, they simply stop being credited the way the user hoped. Nothing breaks. This is why the interface states what depends on Garmin's processing.

**The feature presents a ride as recorded by hardware the user does not own** →
Nothing here fabricates an identity: the serial number in the file stays the one
the recording device wrote. What changes is the manufacturer and product, which
is the point of the feature. The documentation says plainly what is rewritten.

**Expectations exceed what the feature delivers** → Exercise load is confirmed to
appear: the owner of the reference conversion saw it in Garmin Connect, with the
emulated model shown as the recording device. Recovery time did not appear, but
that file was three years old, and recovery time is a forward-looking figure the
watch computes from recent training — an old activity could not produce one
whatever the file said. So the honest position is that load is demonstrated,
recovery time is untested, and the interface says so next to the setting rather
than implying both.

**Emulation changes what a duplicate looks like to Garmin** → The pipeline's own duplicate protection is keyed on the source file's content, which the transformation never alters, so re-offering the same file is still recognised locally. Garmin's own duplicate detection sees modified bytes and may or may not match; the pipeline already treats a duplicate report as a normal outcome either way.

## Migration Plan

Branch `feat/fit-device-emulation`.

1. Add the software development kit and write the round-trip test first: decode a real Wahoo export, re-encode it without modification, decode the result, and assert the message inventory and field values match. **This test passing is the precondition for everything else.**
2. Implement the catalogue as data, referencing the kit's product enumeration by name.
3. Implement the patch for the three targeted messages, including adding the file creator message when absent.
4. Implement the recording-device-only rule for device information records.
5. Implement output verification by re-decoding.
6. Implement the pass-through cases: non-activity files, feature disabled, settings inconsistent.
7. Add the settings: toggle and device selection.
8. Add the settings page fields and the text stating what the service does and does not compute.
9. Register the transformation with the pipeline.
10. Verify end to end: upload an emulated activity and confirm in Garmin Connect that it is attributed to the selected device.

**Rollback:** revert the branch. Activities uploaded while the feature was active stay as they were uploaded; reverting changes nothing already in Garmin Connect. Settings gain three unknown values, which the settings store ignores.

## Open Questions

- Do real Wahoo exports carry developer data fields, and does the round trip preserve them? Answered by step 1 against actual files.
- Does the file creator message already exist in Wahoo exports, or must it always be added? Answered by step 1.
- Are there device information records in Wahoo files whose role is ambiguous — neither clearly the head unit nor clearly a sensor? If so, the safe reading is to leave them alone.
- Does Garmin Connect display the emulated device name from the product identifier alone, or does it also expect a product name field? Answered at step 10 by looking at the result.
- Does the activity actually contribute to training load after emulation, and does recovery time appear after a device sync? This is the question the whole feature exists for, and it can only be answered by observation over several days. It is recorded as an observation, not as an acceptance criterion, because it depends on Garmin's processing rather than on this code.
