## Context

The README was written for a different application. It presents the project as "a windows desktop version", tells the reader to install a runtime that is no longer used, shows two screenshots of an interface that no longer exists, describes entering a Garmin password that is no longer stored, and warns in capital letters that everything is saved in clear text — which by then is false.

Every preceding change corrected the individual facts it personally invalidated, so the document is not wrong in detail by the time this change runs. What it lacks is a shape: it is still a Windows-only narrative with macOS bolted on, and it still opens by explaining the author's personal problem before telling a visitor what the software does.

This change owns the shape, the screenshots, and the framing. It runs last because every input it needs — final artefact names, verified macOS first-launch wording, the sign-in procedure, the emulation fields — is produced by the changes before it, and several of them explicitly record their outcomes for it.

## Goals / Non-Goals

**Goals:**
- A visitor understands within a few lines what the software does and whether it applies to them.
- A macOS user can install and run the application from the README alone.
- A user enabling device emulation knows what it will and will not produce, before they enable it.
- The security section describes what is actually stored.
- Screenshots show the software as it is.

**Non-Goals:**
- Translating the documentation. English matches the repository and the code.
- A documentation site, a wiki, or generated API documentation.
- A contribution guide. Worth having, not part of this change.
- Rewriting the OpenSpec artefacts into user-facing prose. They are development records, not documentation.
- Marketing. The tone stays factual.

## Decisions

### D1 — The document is restructured around the reader's question, not the author's history

The current README opens with the problem the author had: a Garmin unit died, a Wahoo unit replaced it, Garmin Connect still held the gear totals. That is genuinely why the project exists, and it belongs in the document — just not first.

The order becomes: what this does, what you need, install, set up, how it behaves, what it cannot do, then the background. A visitor deciding whether to keep reading gets the answer in the first screen.

### D2 — Installation splits by operating system, and macOS gets the space it needs

Windows installation is short: download, unpack, run. macOS is not, because the operating system will refuse the first launch and the user needs to know that before it happens rather than after.

The macOS section therefore states the refusal is expected, gives the verified approval steps in the order to try them, names the macOS version the steps were verified against, and explains why the build is not signed. The reason is one sentence: a signing certificate is an annual cost this project does not carry.

Putting this after the download link, where the user hits it, is deliberate. A caveat in a section nobody reads is not a caveat.

### D3 — The security section is rewritten, not deleted

The current text warns that all information is saved in clear text and tells the reader not to use the application if they disagree. After the authentication change that is simply untrue: no password is stored, session tokens are encrypted by the operating system, and settings hold no secret.

Deleting the section would be the easy move and the wrong one — a security statement that disappears reads as a security statement that became inconvenient. It is replaced by an accurate one: what is stored, where per operating system, how it is protected, and what an attacker with access to the user's account could reach. The honest posture is stronger than the old warning, so there is no reason to be coy about it.

### D4 — Device emulation is documented with its limits in the same breath as its benefit

This is the section most likely to generate disappointed issues. The rule is that no sentence describes the benefit without the constraint being visible in the same place.

Concretely: that the file is presented as coming from the selected device; that the Unit ID must be one the user actually owns and cannot be validated by the application; that the service applies its own calculations and ignores performance values carried in the file; and that recovery time is produced by the watch, requiring the account's physiological synchronisation and a device sync afterwards.

The wording comes from what was observed during `fit-device-emulation`, which recorded its outcome for exactly this purpose, rather than from what the feature was hoped to do.

### D5 — Screenshots are taken on both operating systems, after everything else has landed

Two screenshots exist and both show the old WPF interface. They are replaced with images of the Avalonia interface: the main view with its log and counters, and the settings view including the emulation fields.

Taking them on both operating systems rather than one is worth the extra minutes: a macOS user who sees only Windows screenshots reasonably wonders whether macOS is an afterthought, and the answer this project wants to give is no.

Screenshots are the first thing to rot. They are taken at the very end, from the artefacts the release pipeline actually produced, not from a development build.

### D6 — Known limitations are listed, not buried

A short section states what the application does not do: no Linux build, notification delivery on macOS subject to what `avalonia-ui-port` observed, re-authentication roughly monthly, and the dependency on an interface Garmin does not publish and may change again — as it did in March 2026.

The last point is the one users most need, because when Garmin next changes something, the difference between "this project is dead" and "this is a known failure mode with a known response" is whether it was written down in advance.

### D7 — The star history link is corrected as part of this change

It uses a retired anchor format. One line, no ceremony, folded in here because this change owns the document.

### D8 — Every instruction is executed before it ships

Every command, every path, every step in the document is run on the platform it targets. The instruction that can be written from memory and quietly stop working is exactly the one that wastes a user's evening.

This applies particularly to the macOS approval steps, which change between operating system versions, and to the checksum verification commands, which differ per platform.

## Risks / Trade-offs

**The README is rewritten before the last observations arrive** → Two inputs are only knowable late: whether emulation actually produces training load and recovery time, and whether notifications are delivered from an unsigned bundle. Both are recorded by their own changes specifically for this one. If either is still unanswered when this change runs, the document states the uncertainty rather than guessing — an honest "we do not know yet" ages better than a confident claim that turns out false.

**macOS instructions rot with the next operating system release** → Unavoidable. Mitigated by naming the version they were verified against, so a reader on a newer system knows the instructions may be stale rather than assuming their download is broken.

**Screenshots rot faster than text** → Mitigated by taking them last, and by keeping them to two rather than illustrating every screen. Fewer images is fewer things to go stale.

**The security section overstates the protection** → Encrypted at rest by the operating system is a real improvement over a password in clear text, and it is not a defence against malware running as the user. The section says what it protects against and what it does not, because an overstated security claim is worse than the old honest warning it replaces.

**Restructuring loses content that mattered** → The author's original motivation, the Dropbox setup path, and the Wahoo companion app steps are all still needed. Restructuring means reordering and rewriting, not discarding; the Wahoo and Dropbox setup instructions in particular are still the part that gets a new user working.

## Migration Plan

Branch `docs/readme-overhaul`, after `cross-platform-packaging-ci`.

1. Collect the recorded outcomes: artefact names and first-launch wording from packaging, emulation observations, the notification answer, token lifetimes for the re-authentication cadence.
2. Draft the new structure and confirm it before writing prose.
3. Write the overview, requirements, and Wahoo and Dropbox setup sections, carrying over what still applies.
4. Write the Windows installation section and run it on a real machine.
5. Write the macOS installation section and run it on a real machine, from a downloaded artefact carrying the quarantine attribute.
6. Write the sign-in section, including two-step verification and the monthly cadence.
7. Write the device emulation section per D4.
8. Write the security section per D3.
9. Write the known limitations section.
10. Take the screenshots on both operating systems from released artefacts.
11. Fix the star history link.
12. Execute every command and step in the finished document, on the platform it targets.
13. Have someone who has never used the application follow it end to end.

**Rollback:** revert the branch. Documentation has no runtime effect, so a rollback restores the previous text with no other consequence — the one case in this project where reverting is genuinely free.

## Open Questions

- Did emulation produce training load and recovery time in practice? Comes from `fit-device-emulation`; if unanswered, the text states the uncertainty.
- Are notifications delivered on macOS from the shipped bundle? Comes from `avalonia-ui-port` and `cross-platform-packaging-ci`.
- What is the real re-authentication cadence, as opposed to the advertised token lifetime? Comes from `garmin-di-oauth2-core`, and may only be answerable after weeks of use.
- Should the hosted Dropbox dependency be revisited in the documentation — is Dropbox still the only path from a Wahoo device, or do current Wahoo applications offer another export route? Worth checking rather than copying the old instructions forward unexamined.
