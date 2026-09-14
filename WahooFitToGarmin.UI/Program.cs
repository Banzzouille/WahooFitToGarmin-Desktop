using Avalonia;

namespace WahooFitToGarmin.UI;

internal static class Program
{
    // Avalonia needs this entry point before any of its types are referenced,
    // so nothing here may touch Application directly.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
