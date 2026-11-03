using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chucao.Views;

internal static class NativeWindowColors
{
    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) => Apply(window);
        window.Activated += (_, _) => Apply(window);
        window.Deactivated += (_, _) => Apply(window);
    }

    private static void Apply(Window window)
    {
        // The native border/caption color attributes require Windows 11 build 22000 or later.
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return;
        nint handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
            return;
        SetColor(window, handle, 34, "WindowFrameColor");
        SetColor(window, handle, 35, "WindowCaptionColor");
        SetColor(window, handle, 36, "TextColor");
    }

    private static void SetColor(Window window, nint handle, int attribute, string resourceKey)
    {
        if (window.TryFindResource(resourceKey) is not Color color)
            return;
        uint nativeColor = (uint)(color.R | (color.G << 8) | (color.B << 16));
        // An unsupported OS attribute keeps the system default without changing window behavior.
        _ = DwmSetWindowAttribute(handle, attribute, ref nativeColor, sizeof(uint));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint handle, int attribute, ref uint value, int size);
}
