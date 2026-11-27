using System.Windows;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// Where the always-on-top panel goes: its saved position, pushed inside the screen holding most of it (so it never
/// sits under the taskbar), or the primary corner when it is on no screen at all.
/// </summary>
internal static class WindowPlacement
{
    private const double Margin = 12;

    /// <param name="saved">Last saved top-left corner, in device-independent pixels.</param>
    /// <param name="workAreas">Work areas of the screens (the primary one first), in device-independent pixels.</param>
    /// <param name="size">Size of the panel.</param>
    public static Point Fit(Point? saved, IReadOnlyList<Rect> workAreas, Size size)
    {
        if (saved is { } position)
        {
            var panel = new Rect(position, size);
            var area = workAreas.MaxBy(area => Overlap(area, panel));
            if (Overlap(area, panel) > 0)
            {
                return new Point(Within(position.X, area.Left, area.Right - size.Width), Within(position.Y, area.Top, area.Bottom - size.Height));
            }
        }

        var primary = workAreas[0];
        return new Point(primary.Right - size.Width - Margin, primary.Bottom - size.Height - Margin);
    }

    private static double Overlap(Rect area, Rect panel)
    {
        var common = Rect.Intersect(area, panel);
        return common.IsEmpty ? 0 : common.Width * common.Height;
    }

    /// <summary>Clamps to [min, max], preferring min when the panel is larger than the area.</summary>
    private static double Within(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
