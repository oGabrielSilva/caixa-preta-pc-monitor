using System.Windows;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// One drag of the panel's corner grip: the window follows the cursor and keeps the panel's proportions, so the side
/// that changed most since the drag began sets the scale (growing or shrinking). The tray app runs this itself instead
/// of the native resize border, because a window with that border is taken over by Snap when dragged to a screen corner.
/// </summary>
/// <param name="startCursor">Cursor position when the drag began, in screen pixels.</param>
/// <param name="startWindow">Window size when the drag began, in device-independent pixels.</param>
/// <param name="content">The page's content size at zoom 1.</param>
internal sealed class ResizeDrag(Point startCursor, Size startWindow, Size content)
{
    /// <summary>
    /// The scale for the cursor at <paramref name="cursor"/> (screen pixels) on a screen at <paramref name="dpiScale"/>;
    /// 1 until the content has been measured.
    /// </summary>
    public double ScaleAt(Point cursor, double dpiScale)
    {
        if (content.Width <= 0 || content.Height <= 0)
        {
            return 1.0;
        }

        var start = startWindow.Width / content.Width;
        var byWidth = (startWindow.Width + ((cursor.X - startCursor.X) / dpiScale)) / content.Width;
        var byHeight = (startWindow.Height + ((cursor.Y - startCursor.Y) / dpiScale)) / content.Height;
        return MiniPanelSize.Clamp(Math.Abs(byWidth - start) >= Math.Abs(byHeight - start) ? byWidth : byHeight);
    }
}
