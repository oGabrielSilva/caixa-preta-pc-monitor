using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using PcMonitor.Tray.Settings;
using Screen = System.Windows.Forms.Screen;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// The always-on-top panel (<c>/mini</c>): no title bar, no taskbar button, dragged by the icon, position and size
/// remembered. A WebView2 inside a WPF window cannot be transparent, so the window takes exactly the size the page
/// reports for its content, times the user's scale (<see cref="MiniPanelSize"/>), which is also the page zoom.
/// The user changes the scale by dragging the page's corner grip (<see cref="ResizeDrag"/>) or with Ctrl + mouse wheel.
/// The window has no resize border of its own: with one, Snap resizes it when it is dragged to a screen corner.
/// </summary>
public partial class MiniWindow
{
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 2;
    private const int VkLButton = 0x01;
    private const int VkRButton = 0x02;

    private readonly TraySettings _settings;
    private readonly DispatcherTimer _resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(15) };
    private ResizeDrag? _resize;
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
            DragRequested = StartMove,
            ResizeRequested = StartResize,
            ScaleStepRequested = OnScaleStep,
            SizeReported = OnContentMeasured,
        };
        _ = host.InitializeAsync(environment);
        _resizeTimer.Tick += (_, _) => ContinueResize();
        // Windows gives the window a default position first: only moves after the initial placement are remembered.
        SourceInitialized += (_, _) =>
        {
            Place(_settings.MiniPanelPosition);
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

        Resize();
        // Never moved: stay anchored to the corner. Moved: keep the top-left corner unless the panel would leave the screen.
        Place(_settings.MiniPanelPosition is null ? null : new Point(Left, Top));
    }

    private void Resize()
    {
        var size = MiniPanelSize.Window(_content, _scale);
        (Width, Height) = (size.Width, size.Height);
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

    /// <summary>Lets Windows move the window as if its title bar were dragged (the mouse button is still down).</summary>
    private void StartMove()
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(handle, WmNcLButtonDown, HtCaption, IntPtr.Zero);
    }

    /// <summary>Follows the cursor while the grip is held (the page asked on mouse down); the top-left corner stays put.</summary>
    private void StartResize()
    {
        if (_content.IsEmpty || !NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        _resize = new ResizeDrag(new Point(cursor.X, cursor.Y), new Size(Width, Height), _content);
        _resizeTimer.Start();
    }

    private void ContinueResize()
    {
        if (_resize is null || !NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        SetScale(_resize.ScaleAt(new Point(cursor.X, cursor.Y), VisualTreeHelper.GetDpi(this).DpiScaleX));
        Resize();
        // GetAsyncKeyState reads the physical buttons: the primary one is the right one when they are swapped.
        var primary = SystemParameters.SwapButtons ? VkRButton : VkLButton;
        if ((NativeMethods.GetAsyncKeyState(primary) & 0x8000) == 0)
        {
            _resizeTimer.Stop();
            _resize = null;
            ApplySize();
            SaveScale();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private static partial class NativeMethods
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReleaseCapture();

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        public static partial IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetCursorPos(out NativePoint point);

        [LibraryImport("user32.dll")]
        public static partial short GetAsyncKeyState(int key);
    }
}
