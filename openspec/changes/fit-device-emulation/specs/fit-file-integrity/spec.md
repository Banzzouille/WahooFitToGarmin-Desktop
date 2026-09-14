## ADDED Requirements

### Requirement: The source file on disk is never modified

Transformation SHALL operate on the activity's content in memory. The file in the watched folder SHALL be byte-for-byte unchanged by the transformation, whatever its outcome.

#### Scenario: The source is unchanged after a successful transformation

- **WHEN** an activity is transformed and uploaded
- **THEN** the file in the watched folder is byte-for-byte identical to what it was before

#### Scenario: The source is unchanged after a failed transformation

- **WHEN** transformation fails
- **THEN** the file in the watched folder is byte-for-byte identical to what it was before

#### Scenario: No temporary file is produced

- **WHEN** a transformation runs
- **THEN** no temporary or intermediate file is written to the watched folder or elsewhere

### Requirement: The produced file is a valid activity file

The bytes produced by transformation SHALL form a structurally valid FIT file: a correct header, correctly declared messages, and a checksum computed over the produced content.

#### Scenario: The output decodes successfully

- **WHEN** a transformed activity is produced
- **THEN** decoding it succeeds without error

#### Scenario: The checksum matches the produced content

- **WHEN** a transformed activity is produced
- **THEN** its checksum is correct for its content, not carried over from the source

#### Scenario: The service accepts the produced file

- **WHEN** a transformed activity is uploaded
- **THEN** it is accepted rather than rejected as malformed

### Requirement: Content outside the emulated identity is preserved

Everything the transformation does not target SHALL survive it. Records, laps, sessions, events, heart rate variability data, the activity summary, and any message the implementation does not specifically modify SHALL be present in the produced file with the same values as in the source.

#### Scenario: Activity data is preserved

- **WHEN** a transformed activity is compared with its source
- **THEN** every data record, lap, session, and event is present with identical values

#### Scenario: Message inventory is preserved

- **WHEN** a transformed activity is compared with its source
- **THEN** the set of messages it contains is the same, apart from a creator record that may have been added

#### Scenario: Unrecognised content is preserved

- **WHEN** the source contains messages the implementation does not model
- **THEN** those messages are present unchanged in the produced file

#### Scenario: Developer data is preserved

- **WHEN** the source contains developer-defined fields
- **THEN** those field definitions and their values are present in the produced file

### Requirement: Output is verified before it is uploaded

The application SHALL verify the bytes it produced before handing them on, by decoding them and confirming that the intended values are present and that nothing else was lost.

#### Scenario: Verification runs on every transformation

- **WHEN** any activity is transformed
- **THEN** the produced bytes are decoded and checked, not only during testing

#### Scenario: Intended values are confirmed

- **WHEN** verification runs
- **THEN** it confirms that the manufacturer, product, and serial number hold the configured values

#### Scenario: Loss is detected

- **WHEN** the produced file is missing content that the source contained
- **THEN** verification fails and the produced bytes are not uploaded

### Requirement: A failed transformation fails the activity rather than uploading

When transformation or its verification fails, the application SHALL report the activity as failed, SHALL retain the source file, and SHALL NOT upload the original bytes as a fallback.

#### Scenario: Failure is reported

- **WHEN** transformation fails for any reason
- **THEN** the activity is counted as failed and the reason is logged

#### Scenario: The source file is retained

- **WHEN** transformation fails
- **THEN** the source file is not deleted, regardless of the keep-uploaded-file setting

#### Scenario: No silent fallback to the original

- **WHEN** transformation fails while emulation is enabled
- **THEN** the application does not quietly upload the untransformed file instead

#### Scenario: Preservable content is not sacrificed to succeed

- **WHEN** the content of a source file cannot be carried through the transformation intact
- **THEN** the transformation fails rather than producing a file with content removed

### Requirement: Files that are not activities pass through unchanged

A FIT file that is not an activity SHALL be uploaded with its original content, and the fact SHALL be logged. This SHALL NOT be treated as an error.

#### Scenario: A non-activity file is not emulated

- **WHEN** a FIT file that is not an activity is processed while emulation is enabled
- **THEN** its bytes are uploaded unchanged

#### Scenario: The pass-through is visible

- **WHEN** a non-activity file passes through
- **THEN** the log states that emulation did not apply and why

#### Scenario: Pass-through is not a failure

- **WHEN** a non-activity file passes through
- **THEN** the failure count is unchanged

### Requirement: Transformation is independent of the file system

The transformation SHALL be expressible as content in, content out, with no knowledge of where the activity came from, so that it can be exercised in tests without a file system.

#### Scenario: Transformation runs from memory alone

- **WHEN** the transformation is invoked in a test with a byte array
- **THEN** it produces a result without touching the file system

#### Scenario: Round-trip fidelity is demonstrable

- **WHEN** a real activity file is decoded and re-encoded with no modification requested
- **THEN** the result decodes to the same message inventory and values as the original
