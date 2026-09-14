using System.IO;
using System.Windows.Forms;

using WahooFitToGarmin_Desktop.Core.Platform;

namespace WahooFitToGarmin_Desktop.Services.Platform
{
    /// <summary>
    /// Folder selection through the Windows Forms dialog.
    /// </summary>
    /// <remarks>
    /// Replaced by the Avalonia storage provider in <c>avalonia-ui-port</c>,
    /// which is also what finally removes the Windows Forms reference.
    /// </remarks>
    public sealed class WpfFolderPicker : IFolderPicker
    {
        public Task<string?> PickFolderAsync(string? initialPath = null)
        {
            using var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };

            if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
            {
                dialog.SelectedPath = initialPath;
            }

            var result = dialog.ShowDialog();

            return Task.FromResult(result == DialogResult.OK ? dialog.SelectedPath : null);
        }
    }
}
