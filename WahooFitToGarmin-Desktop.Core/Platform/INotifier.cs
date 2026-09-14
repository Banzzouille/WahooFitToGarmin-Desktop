namespace WahooFitToGarmin_Desktop.Core.Platform
{
    /// <summary>
    /// Raises a notification to the user through whatever the host platform
    /// offers.
    /// </summary>
    /// <remarks>
    /// Implementations must treat delivery failure as recoverable: a platform
    /// that refuses to show a notification must not interrupt the upload
    /// pipeline.
    /// </remarks>
    public interface INotifier
    {
        void Notify(string title, string body);
    }
}
