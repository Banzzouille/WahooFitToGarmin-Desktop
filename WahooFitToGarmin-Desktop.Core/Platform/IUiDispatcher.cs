namespace WahooFitToGarmin_Desktop.Core.Platform
{
    /// <summary>
    /// Marshals work onto the thread the user interface requires.
    /// </summary>
    /// <remarks>
    /// Exists so that code in this library can update bound state without
    /// knowing which user interface framework is hosting it.
    /// </remarks>
    public interface IUiDispatcher
    {
        bool IsOnUiThread { get; }

        void Post(Action action);
    }
}
