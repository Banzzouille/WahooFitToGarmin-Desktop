using System.Security.Cryptography;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Identity of an activity, derived from its content.
    /// </summary>
    /// <remarks>
    /// Hashing the content rather than keying on a path or a file name is what
    /// makes the guarantee exact. A path does not survive the sync client
    /// re-downloading the file, the folder being moved, or the file being
    /// renamed; a name alone collides. Activity files are small, so hashing
    /// costs nothing measurable, and the bytes are already in memory because
    /// the upload needs them.
    /// </remarks>
    public static class ActivityHash
    {
        public static string Compute(byte[] content) =>
            Convert.ToHexStringLower(SHA256.HashData(content));
    }
}
