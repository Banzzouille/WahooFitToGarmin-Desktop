namespace WahooFitToGarmin_Desktop.Core.Platform
{
    /// <summary>
    /// Asks the user to choose a folder, using the host platform's own dialog.
    /// </summary>
    public interface IFolderPicker
    {
        /// <summary>
        /// Returns the chosen path, or null when the user cancelled.
        /// </summary>
        Task<string?> PickFolderAsync(string? initialPath = null);
    }
}
