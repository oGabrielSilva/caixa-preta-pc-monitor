using System.Windows;
using PcMonitor.Tray.Windows;

namespace PcMonitor.Tray.Tests;

public sealed class ResizeDragTests
{
    private static readonly Size _content = new(280, 40);

    [Fact]
    public void Moving_the_grip_grows_the_window_by_the_distance_the_cursor_travelled()
    {
        var drag = new ResizeDrag(new Point(1000, 500), MiniPanelSize.Window(_content, 1.0), _content);

        drag.ScaleAt(new Point(1140, 500), dpiScale: 1.0).ShouldBe(1.5);
    }

    [Fact]
    public void The_cursor_distance_is_converted_from_screen_pixels_to_window_units()
    {
        var drag = new ResizeDrag(new Point(1000, 500), MiniPanelSize.Window(_content, 1.0), _content);

        drag.ScaleAt(new Point(1280, 500), dpiScale: 2.0).ShouldBe(1.5);
    }

    [Fact]
    public void Moving_the_grip_back_shrinks_the_window_down_to_the_smallest_scale()
    {
        var drag = new ResizeDrag(new Point(1000, 500), MiniPanelSize.Window(_content, 2.0), _content);

        drag.ScaleAt(new Point(860, 500), dpiScale: 1.0).ShouldBe(1.5);
        drag.ScaleAt(new Point(0, 0), dpiScale: 1.0).ShouldBe(MiniPanelSize.MinScale);
    }

    [Fact]
    public void Dragging_diagonally_follows_the_side_that_changed_most_and_keeps_the_proportions()
    {
        var drag = new ResizeDrag(new Point(1000, 500), MiniPanelSize.Window(_content, 1.0), _content);

        drag.ScaleAt(new Point(1140, 505), dpiScale: 1.0).ShouldBe(1.5);
        drag.ScaleAt(new Point(1020, 520), dpiScale: 1.0).ShouldBe(1.5);
        drag.ScaleAt(new Point(9000, 500), dpiScale: 1.0).ShouldBe(MiniPanelSize.MaxScale);
    }

    [Fact]
    public void A_panel_without_a_measured_content_keeps_its_scale()
    {
        var drag = new ResizeDrag(new Point(1000, 500), new Size(280, 40), new Size(0, 0));

        drag.ScaleAt(new Point(1100, 560), dpiScale: 1.0).ShouldBe(1.0);
    }
}
