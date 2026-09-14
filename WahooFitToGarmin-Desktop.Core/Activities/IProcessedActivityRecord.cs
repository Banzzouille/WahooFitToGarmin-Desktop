namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// How an activity ended up.
    /// </summary>
    public enum ActivityOutcome
    {
        /// <summary>Present when the record was first created, and never uploaded.</summary>
        Baseline,

        Uploaded,

        /// <summary>The service already held this activity.</summary>
        Duplicate,
    }

    /// <summary>
    /// Durable record of activities already dealt with, so none is uploaded
    /// twice.
    /// </summary>
    /// <remarks>
    /// Keyed on a hash of the file's content rather than its path or name:
    /// neither survives the file being re-downloaded by a sync client, the
    /// folder being moved, or the file being renamed.
    /// </remarks>
    public interface IProcessedActivityRecord
    {
        /// <summary>
        /// True when no record exists yet, which is how the first run after this
        /// capability is introduced is detected.
        /// </summary>
        bool IsFirstRun { get; }

        bool Contains(string contentHash);

        void Mark(string contentHash, string fileName, ActivityOutcome outcome);

        /// <summary>
        /// Records many entries at once without uploading them, used to
        /// establish the first-run baseline.
        /// </summary>
        void MarkBaseline(IReadOnlyCollection<(string ContentHash, string FileName)> entries);
    }
}
