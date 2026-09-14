using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WahooFitToGarmin.UI.Services.Platform;

/// <summary>
/// Shows or hides the macOS Dock icon at runtime.
/// </summary>
/// <remarks>
/// The application keeps watching the folder after its window is closed, so it
/// should behave like the background utility it is: present in the menu bar,
/// absent from the Dock and from the application switcher, until its window is
/// shown again.
///
/// The static way to get that is <c>LSUIElement</c> in the bundle's property
/// list, but it is all or nothing: the icon would never appear, not even while
/// the user has the window open and is working in it. Switching the activation
/// policy gives the behaviour people actually expect from this kind of tool.
///
/// Everything here is a no-op away from macOS.
/// </remarks>
public static class MacOsDockVisibility
{
    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
    private const string ObjC = "/usr/lib/libobjc.dylib";

    /// <summary>Ordinary application: Dock icon, application switcher, menu bar.</summary>
    private const long PolicyRegular = 0;

    /// <summary>Background utility: menu bar only.</summary>
    private const long PolicyAccessory = 1;

    public static void Set(bool visibleInDock)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        try
        {
            Apply(visibleInDock ? PolicyRegular : PolicyAccessory);
        }
        catch (Exception)
        {
            // A cosmetic detail is never worth taking the application down. If
            // the runtime refuses the interop, the icon simply stays as it is.
        }
    }

    [SupportedOSPlatform("macos")]
    private static void Apply(long policy)
    {
        var application = objc_getClass("NSApplication");
        if (application == IntPtr.Zero)
        {
            return;
        }

        var shared = objc_msgSend(application, sel_registerName("sharedApplication"));
        if (shared == IntPtr.Zero)
        {
            return;
        }

        objc_msgSend_long(shared, sel_registerName("setActivationPolicy:"), policy);
    }

    [DllImport(ObjC, EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC, EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_long(IntPtr receiver, IntPtr selector, long argument);
}
