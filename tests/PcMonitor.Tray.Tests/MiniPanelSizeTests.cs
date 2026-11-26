using System.Windows;
using PcMonitor.Tray.Windows;

namespace PcMonitor.Tray.Tests;

public sealed class MiniPanelSizeTests
{
    private static readonly Size _content = new(280, 40);

    [Fact]
    public void The_window_is_the_content_at_the_chosen_scale()
    {
        MiniPanelSize.Window(_content, 1.5).ShouldBe(new Size(420, 60));
    }

    [Theory]
    [InlineData(0.2, MiniPanelSize.MinScale)]
    [InlineData(9.0, MiniPanelSize.MaxScale)]
    [InlineData(1.25, 1.25)]
    [InlineData(double.NaN, 1.0)]
    public void The_scale_stays_within_its_limits(double scale, double expected)
    {
        MiniPanelSize.Clamp(scale).ShouldBe(expected);
    }

    [Fact]
    public void Dragging_the_corner_follows_the_side_that_grew_most_and_keeps_the_proportions()
    {
        MiniPanelSize.ScaleFor(new Size(420, 45), _content).ShouldBe(1.5);
        MiniPanelSize.ScaleFor(new Size(300, 60), _content).ShouldBe(1.5);
        MiniPanelSize.ScaleFor(new Size(5000, 40), _content).ShouldBe(MiniPanelSize.MaxScale);
    }

    [Fact]
    public void A_panel_without_a_measured_content_keeps_its_scale()
    {
        MiniPanelSize.ScaleFor(new Size(300, 60), new Size(0, 0)).ShouldBe(1.0);
    }

    [Fact]
    public void Each_wheel_step_changes_the_scale_by_an_eighth_within_the_limits()
    {
        MiniPanelSize.Step(1.0, 1).ShouldBe(1.125);
        MiniPanelSize.Step(1.0, -1).ShouldBe(0.875);
        MiniPanelSize.Step(MiniPanelSize.MaxScale, 1).ShouldBe(MiniPanelSize.MaxScale);
    }
}
