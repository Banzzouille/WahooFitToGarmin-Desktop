# Round-trip gate — findings

Task group 2 asked one question: does decoding a real Wahoo export and
re-encoding it lose anything? Two files answered it, both supplied by the
project owner:

- `2022-06-21-193152-ELEMNT BOLT 9635-282-0.fit` — an ELEMNT BOLT ride
- `22 june 2022 fit file changed to xt910.fit` — the same ride, already
  converted to a Garmin Forerunner 910XT by another tool

The second is the more valuable of the two: it is a worked example of the
conversion this change has to reproduce.

## What a real Wahoo export contains

| | |
|---|---:|
| Messages | 8215 |
| Message types | 16 |
| Developer field values | 16 190 |
| Messages carrying developer fields | 8112 |
| Unrecognised messages | 915 |

The unrecognised messages are Wahoo's own: types 65280, 65281 and 65285. A
decoder that drops what it does not recognise would discard 915 of them, and a
re-encode that ignores developer field definitions would discard sixteen
thousand values. This is exactly what the gate existed to check.

## The gate passes

Decoding and re-encoding without modification preserves:

- the message inventory, exactly — 8215 messages, same types, same counts
- all 915 unrecognised messages
- the developer fields

The developer field count reads 16 190 from the source and 16 192 from the
round-tripped output. That is not a gain of content: two `DeviceInfo` messages
each carry two developer fields sharing one identity, and re-encoding separates
what the source had collapsed. Nothing is lost either way.

**A correction worth recording**: an earlier measurement reported two fields
*lost*, and that was wrong. It compared an in-memory list taken before encoding
against a re-decode taken after — two different things. Measuring all files the
same way, by decoding from bytes, gives the result above.

## The output is larger, and grows if reprocessed

A single round trip takes the file from 317 123 to 374 827 bytes, about
eighteen percent. The reference conversion produced 375 052 bytes from the same
source — within 0.06% of ours, which strongly suggests that tool did the same
decode and re-encode.

Repeating the round trip keeps growing the file: 317 k, 377 k, 435 k, 492 k,
while the message and field counts stay fixed. A definition record is emitted
ahead of each of the 8112 messages carrying developer fields rather than being
reused, and the cost compounds.

This does not affect us in normal operation, because the transformation is
applied once, to a source file, and the output is uploaded rather than kept. It
does mean the transformation must never be applied to its own output. The
idempotence record already prevents an activity being processed twice, so the
requirement is met — but it is now a stated reason rather than an accident.

## What the reference conversion changed

Only identity, and not where the design expected.

| Field | Wahoo original | Converted |
|---|---|---|
| `FileId.manufacturer` | 32 (Wahoo Fitness) | 1 (Garmin) |
| `FileId.product` | 31 | 1328 (Forerunner 910XT) |
| `FileId.serialNumber` | 2519185680 | unchanged |
| `FileId.timeCreated` | 2022-06-21 19:31:53Z | unchanged |
| `FileCreator` | absent | still absent |
| `DeviceInfo`, every record | manufacturer 32 and 51 | all set to 1 / 1328 |

Three things follow, and two of them contradict decisions written before any
file had been seen.

**The serial number was kept, not replaced.** The design has the user supply
their own device Unit ID and writes it into the file. The reference conversion
left the Wahoo unit's serial in place and changed only manufacturer and product.

**Every device record was rewritten, including sensors.** Design decision D3
says only the recording device is touched, on the grounds that claiming a
user's power meter is a Garmin device is false and breaks sensor attribution.
The reference tool rewrote the power meter — manufacturer 51, device type 11 —
along with everything else.

**No file creator message was added.** The design calls for adding one when
absent. The reference conversion did not, and the file was evidently usable.

None of this proves the reference tool is right. It proves it is what somebody
did, and that the result was worth keeping. Whether it was *accepted* by the
service, and whether it produced training load, is the question that decides
which behaviour to implement — and it is a question only the file's owner can
answer.

## Recorded outcomes

Answers to the questions group 10 asked, from the fixtures and from running the
application against them.

**Do Wahoo exports carry developer data fields, and do they survive?**
Yes, and yes. The ELEMNT BOLT export carries 16 190 developer field values across
8112 of its 8215 messages, declared by fifteen developer data identifiers and
sixteen field descriptions. All of it survives. This was the single largest risk
in the approach and it is retired.

**Do they already contain a file creator message?**
No. Neither the original nor the conversion the service credited has one, which
is why none is written. An earlier design added one; doing so would have invented
a requirement.

**Was any device record ambiguous?**
No. The export carries 84 device records: the head unit at index 0, and sensors
at other indexes with their own manufacturers — 51 for the power meter — their
own serial numbers and their own device types. Nothing needed a judgement call,
because the decision changed: every record has its manufacturer and product
rewritten, and everything that identifies an individual sensor is left alone.

**Does the emulated device name display correctly from the product identifier
alone?**
Yes. The owner of the reference conversion confirmed the model appears in Garmin
Connect as the recording device, with nothing in the file naming it but the
product identifier. No product name string is needed.

**Does emulation produce training load, and recovery time?**
Exercise load: observed. The owner confirmed it in Garmin Connect for the
converted file.

Recovery time: not observed, and not concluded either way. That file records a
ride from June 2022. Recovery time is a forward-looking figure the watch computes
from recent training, so a three-year-old activity could not have produced one
whatever the file said. The honest statement, which is what the interface makes,
is that load is demonstrated and recovery time is untested.

Testing it properly means emulating a recent ride and syncing the watch
afterwards, which needs the authentication work to land first.

## Verified in the running application

On macOS, with emulation enabled and an Edge 1040 selected, a real ELEMNT BOLT
export dropped into the watched folder produced:

```
A new file is coming => ride.fit
ride.fit presented as Garmin Edge 1040, 85 identity records rewritten
Failed to upload ride.fit : no credentials configured
```

Eighty five records: the file identifier and all 84 device records, which is what
the reference conversion changed.

The upload failure is expected — authentication is the next change. Two things
about it are worth recording, because both are behaviours that had to be designed
for rather than assumed:

- the source file on disk is byte-for-byte identical afterwards
- the activity was not written to the processed record, so it remains retryable
  rather than being silently considered done
