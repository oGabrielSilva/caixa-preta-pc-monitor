using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using PcMonitor.Tray.Settings;
using Screen = System.Windows.Forms.Screen;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// The always-on-top panel (<c>/mini</c>): no title bar, no taskbar button, dragged by the icon, position remembered.
/// A WebView2 inside a WPF window cannot be transparent, so the window is exactly the size of the panel.
/// </summary>
public partial class MiniWindow
{
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 2;

    private readonly TraySettings _settings;

    internal MiniWindow(CoreWebView2Environment environment, Uri dashboard, TraySettings settings, App app)
    {
        InitializeComponent();
        _settings = settings;
        var host = new WebViewHost(View, dashboard, "/mini")
        {
            OpenRequested = path => app.ShowMain(path),
            DragRequested = StartDrag,
        };
        _ = host.InitializeAsync(environment);
        // Windows gives the window a default position first: only moves after the initial placement are remembered.
        SourceInitialized += (_, _) =>
        {
            Place();
            LocationChanged += (_, _) => Remember();
        };
    }

    private void Place()
    {
        var position = WindowPlacement.Fit(_settings.MiniPanelPosition, WorkAreas(), new Size(Width, Height));
        (Left, Top) = (position.X, position.Y);
    }

    /// <summary>The screens' work areas in device-independent pixels, the primary one first.</summary>
    private Rect[] WorkAreas()
    {
        var toDips = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        return [.. Screen.AllScreens
            .OrderByDescending(screen => screen.Primary)
            .Select(screen => screen.WorkingArea)
            .Select(area => new Rect(toDips.Transform(new Point(area.Left, area.Top)), toDips.Transform(new Point(area.Right, area.Bottom))))];
    }

    private void Remember()
    {
        _settings.MiniPanelPosition = new Point(Left, Top);
        _settings.Save(TraySettings.DefaultPath);
    }

    /// <summary>Lets Windows move the window as if its caption were dragged (the mouse button is still down).</summary>
    private void StartDrag()
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(handle, WmNcLButtonDown, HtCaption, IntPtr.Zero);
    }

    private static partial class NativeMethods
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReleaseCapture();

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        public static partial IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    }
}
