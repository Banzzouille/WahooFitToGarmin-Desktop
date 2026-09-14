# Open questions, settled

The design left four questions to be answered during implementation rather than
guessed at up front. This is what was decided and why, so that none of them is
silently revisited later.

## Readiness timings — fixed constants, not settings

**Quiet interval:** one second.
**Consecutive unchanged observations required:** two.
**Abandon after:** two minutes.

Recorded in the `activity-watch-pipeline` specification, so a change to any of
them is a change to the specification rather than a tweak.

They are not exposed as settings. A user cannot reason about what a quiet
interval should be, and the value of the mechanism is that it behaves the same
way every time.

Two observations rather than one is the part that matters. A single check
reports a file as settled during any pause in its download, which is how a
partial file gets uploaded. There is a test that fails with one check and passes
with more.

**Still to confirm:** these were chosen against a desktop sync client's write
pattern, reasoned rather than measured. Group 10 verifies them against a real
folder on Windows, and `avalonia-ui-port` carries the equivalent task for macOS,
where file system events behave differently.

## Record retention — by count, oldest first

**2000 entries, oldest removed first.**

By count rather than by age. Age would have to assume something about how often
a user rides, and would silently forget activities for someone returning after a
winter off. A count bounds the file, which is the actual goal, and 2000 entries
is more history than the sync client will ever re-offer.

An activity old enough to fall off the end is not one anything is still
offering. If it somehow is, the service's own duplicate detection catches it and
the file is retained rather than deleted — the failure mode is noise, not data
loss.

## An explicit "import files already present" action — not raised

**Decided: no, and not carried forward as follow-up work.**

The first run marks existing files as seen rather than uploading them, because
the alternative pushes a user's entire back catalogue to the service at once.
The obvious counterpart is an action that says "actually, do upload what is
already there".

It is not added, for two reasons. It is a user interface feature, and this
change deliberately contains none. And nobody has asked for it: the users this
protects are those upgrading with a full folder, for whom the correct behaviour
is exactly what now happens.

The count of baselined files is logged, so a user who wanted those uploaded can
see that they were not, and re-adding a file to the folder re-triggers it. If
the question comes up in practice, `avalonia-ui-port` is where it would belong —
but inventing it now would be building for an imagined user.

## Duplicates never delete the source file — deliberate, do not revisit

**Decided: a duplicate report never deletes the user's file, whatever the
keep-uploaded-file setting says.**

This looks like an inconsistency and will attract a tidy-up at some point. It is
not one.

A duplicate means the service already holds the activity, so deleting the local
file would *probably* be safe. Probably is not a good enough basis for
irreversibly removing someone's data. The cost of being wrong is a lost ride;
the cost of being conservative is a file the user can delete themselves.

The same reasoning covers failures: a failed activity keeps its file, so nothing
is lost when an upload does not go through.

Both are covered by tests that assert nothing was deleted, so a future
simplification fails rather than quietly changing the behaviour.
