using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Tests;

public sealed class SourceStatusTests
{
    [Theory]
    [InlineData(false, true, true, true, SensorSourceStatus.DriverMissing, false)]
    [InlineData(true, false, true, true, SensorSourceStatus.DriverMissing, false)]   // installed, but not running (blocked, pending reboot)
    [InlineData(true, true, false, true, SensorSourceStatus.DriverMissing, false)]   // not elevated: values would be fake zeros
    [InlineData(true, true, true, false, SensorSourceStatus.BoardNotDetected, false)] // no proof the driver reads anything
    [InlineData(true, true, true, true, SensorSourceStatus.Ok, true)]
    public void Driver_backed_sensors_need_the_driver_running_privileges_and_a_detected_board(
        bool driverInstalled, bool driverRunning, bool privileged, bool superIoFound, SensorSourceStatus expected, bool driverUsable)
    {
        var result = SourceStatus.Resolve(driverInstalled, driverRunning, privileged, superIoFound);

        result.ShouldBe((expected, driverUsable));
    }
}
