using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Platform;

namespace WahooFitToGarmin.UI.Services.Platform;

/// <summary>Dispatches onto the Avalonia user interface thread.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool IsOnUiThread => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}

/// <summary>
/// Folder selection through the platform's own picker.
/// </summary>
/// <remarks>
/// Replaces the Windows Forms dialog, which was the last reason that reference
/// existed. The picker is reached from a top level, so this resolves the active
/// window rather than holding one.
/// </remarks>
public sealed class AvaloniaFolderPicker : IFolderPicker
{
    public async Task<string?> PickFolderAsync(string? initialPath = null)
    {
        var storage = TopLevel.GetTopLevel(MainWindow())?.StorageProvider;
        if (storage is null || !storage.CanPickFolder)
        {
            return null;
        }

        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            start = await storage.TryGetFolderFromPathAsync(initialPath);
        }

        var chosen = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });

        return chosen.Count == 0 ? null : chosen[0].TryGetLocalPath();
    }

    private static Window? MainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}

/// <summary>
/// Reports activity to the user.
/// </summary>
/// <remarks>
/// There is no native notification yet. The obvious library for this,
/// DesktopNotifications, was evaluated and rejected: it ships Windows and
/// FreeDesktop backends and none for macOS, so it would not work on the platform
/// this port exists for, and it pulls a transitive dependency carrying a known
/// high-severity advisory. Introducing a vulnerability for a feature that does
/// not work on the target is the wrong trade.
///
/// So notifications are recorded to the log for now. The specification already
/// requires delivery failure to be non-fatal, with the tray icon and the in-app
/// log as the guaranteed channels — which is exactly the state this leaves us
/// in, rather than a silent gap.
///
/// Replacing this means either a library with a real macOS backend, or two small
/// platform implementations: the user notification framework on macOS and the
/// toast API on Windows.
/// </remarks>
public sealed class LoggingNotifier : INotifier
{
    private readonly ILogger<LoggingNotifier> _logger;

    public LoggingNotifier(ILogger<LoggingNotifier> logger) => _logger = logger;

    public void Notify(string title, string body) =>
        _logger.LogInformation("{Title}: {Body}", title, body);
}
