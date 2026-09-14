namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Transforms an activity's content before it is uploaded.
    /// </summary>
    /// <remarks>
    /// The set of transformations is empty in this change. It exists so that
    /// <c>fit-device-emulation</c> can add device identity rewriting by
    /// supplying one member, without restructuring the pipeline.
    ///
    /// Implementations take bytes and return bytes. They never touch the file
    /// system, which is what keeps the source file on disk unmodified by
    /// construction rather than by discipline.
    /// </remarks>
    public interface IActivityTransformation
    {
        /// <summary>
        /// Returns the content to upload. Returning the input unchanged is
        /// valid and means the transformation did not apply.
        /// </summary>
        /// <param name="content">The activity file's content.</param>
        /// <param name="fileName">The original file name, for logging and for
        /// deciding whether the transformation applies.</param>
        byte[] Apply(byte[] content, string fileName);
    }
}
