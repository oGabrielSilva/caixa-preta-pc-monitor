using System.Windows;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// Size of the always-on-top panel: the page's content (measured in CSS pixels at zoom 1) times the user's scale,
/// which is also the WebView2 zoom, so the window always fits the content.
/// </summary>
internal static class MiniPanelSize
{
    public const double MinScale = 0.75;
    public const double MaxScale = 2.5;

    /// <summary>How much one Ctrl + mouse wheel step changes the scale.</summary>
    public const double WheelStep = 0.125;

    /// <summary>The scale after <paramref name="steps"/> wheel steps (positive grows), within the limits.</summary>
    public static double Step(double scale, int steps) => Clamp(scale + (steps * WheelStep));

    public static Size Window(Size content, double scale) => new(content.Width * scale, content.Height * scale);

    /// <summary>The scale kept within its limits; 1 when it is not a number.</summary>
    public static double Clamp(double scale) => double.IsFinite(scale) ? Math.Clamp(scale, MinScale, MaxScale) : 1.0;

    /// <summary>
    /// The scale for a window the user is dragging to <paramref name="proposed"/>: the side that grew (or shrank) most
    /// wins, so the panel keeps its proportions. 1 until the content has been measured.
    /// </summary>
    public static double ScaleFor(Size proposed, Size content) =>
        content.Width > 0 && content.Height > 0
            ? Clamp(Math.Max(proposed.Width / content.Width, proposed.Height / content.Height))
            : 1.0;
}
