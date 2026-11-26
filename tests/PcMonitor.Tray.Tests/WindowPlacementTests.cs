using System.Windows;
using PcMonitor.Tray.Windows;

namespace PcMonitor.Tray.Tests;

public sealed class WindowPlacementTests
{
    private static readonly Rect _primary = new(0, 0, 1920, 1040);
    private static readonly Rect _secondary = new(1920, 0, 1280, 1000);
    private static readonly Size _panel = new(300, 56);

    [Fact]
    public void Without_a_saved_position_the_panel_starts_in_the_primary_bottom_right_corner() =>
        WindowPlacement.Fit(null, [_primary, _secondary], _panel).ShouldBe(new Point(1920 - 300 - 12, 1040 - 56 - 12));

    [Fact]
    public void A_saved_position_inside_a_screen_is_kept() =>
        WindowPlacement.Fit(new Point(2000, 100), [_primary, _secondary], _panel).ShouldBe(new Point(2000, 100));

    [Fact]
    public void A_saved_position_outside_every_screen_moves_back_to_the_primary_work_area() =>
        WindowPlacement.Fit(new Point(3500, 100), [_primary], _panel).ShouldBe(new Point(1920 - 300 - 12, 1040 - 56 - 12));

    [Fact]
    public void A_panel_half_off_the_screen_is_moved_back() =>
        WindowPlacement.Fit(new Point(1800, 100), [_primary], _panel).ShouldBe(new Point(1920 - 300 - 12, 1040 - 56 - 12));
}
