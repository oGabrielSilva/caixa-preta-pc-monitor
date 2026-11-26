using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using PcMonitor.Tray.Settings;
using Screen = System.Windows.Forms.Screen;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// The always-on-top panel (<c>/mini</c>): no title bar, no taskbar button, dragged by the icon, position and size
/// remembered. A WebView2 inside a WPF window cannot be transparent, so the window takes exactly the size the page
/// reports for its content, times the user's scale (<see cref="MiniPanelSize"/>), which is also the page zoom.
/// The user changes the scale by dragging the page's corner grip or with Ctrl + mouse wheel.
/// </summary>
public partial class MiniWindow
{
    private const int WmNcLButtonDown = 0xA1;
    private const int WmSizing = 0x214;
    private const int WmExitSizeMove = 0x232;
    private const int HtCaption = 2;
    private const int HtBottomRight = 17;

    private readonly TraySettings _settings;
    private Size _content = Size.Empty;
    private double _scale;

    internal MiniWindow(CoreWebView2Environment environment, Uri dashboard, TraySettings settings, App app)
    {
        InitializeComponent();
        _settings = settings;
        _scale = MiniPanelSize.Clamp(settings.MiniPanelScale);
        View.ZoomFactor = _scale;
        var host = new WebViewHost(View, dashboard, "/mini")
        {
            OpenRequested = path => app.ShowMain(path),
            DragRequested = () => StartNonClientDrag(HtCaption),
            ResizeRequested = () => StartNonClientDrag(HtBottomRight),
            ScaleStepRequested = OnScaleStep,
            SizeReported = OnContentMeasured,
        };
        _ = host.InitializeAsync(environment);
        // Windows gives the window a default position first: only moves after the initial placement are remembered.
        SourceInitialized += (_, _) =>
        {
            Place(_settings.MiniPanelPosition);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
            LocationChanged += (_, _) => RememberPosition();
        };
    }

    private void OnContentMeasured(Size content)
    {
        _content = content;
        ApplySize();
    }

    private void OnScaleStep(int step)
    {
        SetScale(MiniPanelSize.Step(_scale, step));
        ApplySize();
        SaveScale();
    }

    private void SetScale(double scale)
    {
        _scale = MiniPanelSize.Clamp(scale);
        View.ZoomFactor = _scale;
    }

    /// <summary>Sizes the window to the content at the current scale, keeping it on a screen.</summary>
    private void ApplySize()
    {
        if (_content.IsEmpty)
        {
            return;
        }

        var size = MiniPanelSize.Window(_content, _scale);
        (Width, Height) = (size.Width, size.Height);
        // Never moved: stay anchored to the corner. Moved: keep the top-left corner unless the panel would leave the screen.
        Place(_settings.MiniPanelPosition is null ? null : new Point(Left, Top));
    }

    private void Place(Point? position)
    {
        var placed = WindowPlacement.Fit(position, WorkAreas(), new Size(Width, Height));
        (Left, Top) = (placed.X, placed.Y);
    }

    /// <summary>The screens' work areas in device-independent pixels, the primary one first.</summary>
    private Rect[] WorkAreas()
    {
        var toDips = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        return [.. Screen.AllScreens
            .OrderByDescending(screen => screen.Primary)
            .Select(screen => screen.WorkingArea)
            .Select(area => new Rect(toDips.Transform(new Point(area.Left, area.Top)), toDips.Transform(new Point(area.Right, area.Bottom))))];
    }

    private void RememberPosition()
    {
        _settings.MiniPanelPosition = new Point(Left, Top);
        _settings.Save(TraySettings.DefaultPath);
    }

    private void SaveScale()
    {
        _settings.MiniPanelScale = _scale;
        _settings.Save(TraySettings.DefaultPath);
    }

    /// <summary>While the grip is dragged: keep the proportions and zoom the page with the window.</summary>
    private IntPtr WndProc(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmSizing && !_content.IsEmpty)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var rect = Marshal.PtrToStructure<NativeRect>(lParam);
            var proposed = new Size((rect.Right - rect.Left) / dpi.DpiScaleX, (rect.Bottom - rect.Top) / dpi.DpiScaleY);
            SetScale(MiniPanelSize.ScaleFor(proposed, _content));
            var size = MiniPanelSize.Window(_content, _scale);
            rect.Right = rect.Left + (int)Math.Ceiling(size.Width * dpi.DpiScaleX);
            rect.Bottom = rect.Top + (int)Math.Ceiling(size.Height * dpi.DpiScaleY);
            Marshal.StructureToPtr(rect, lParam, fDeleteOld: false);
            handled = true;
            return 1;
        }

        if (message == WmExitSizeMove)
        {
            SaveScale();
        }

        return IntPtr.Zero;
    }

    /// <summary>Lets Windows move or resize the window as if its frame were dragged (the mouse button is still down).</summary>
    private void StartNonClientDrag(int hitTest)
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(handle, WmNcLButtonDown, hitTest, IntPtr.Zero);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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
