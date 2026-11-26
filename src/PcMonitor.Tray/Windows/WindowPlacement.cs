using System.Windows;

namespace PcMonitor.Tray.Windows;

/// <summary>Where the always-on-top panel goes: its saved position when still on a screen, else the primary corner.</summary>
internal static class WindowPlacement
{
    private const double Margin = 12;

    /// <param name="saved">Last saved top-left corner, in device-independent pixels.</param>
    /// <param name="workAreas">Work areas of the screens (the primary one first), in device-independent pixels.</param>
    /// <param name="size">Size of the panel.</param>
    public static Point Fit(Point? saved, IReadOnlyList<Rect> workAreas, Size size)
    {
        if (saved is { } position && workAreas.Any(area => area.Contains(new Rect(position, size))))
        {
            return position;
        }

        var primary = workAreas[0];
        return new Point(primary.Right - size.Width - Margin, primary.Bottom - size.Height - Margin);
    }
}
