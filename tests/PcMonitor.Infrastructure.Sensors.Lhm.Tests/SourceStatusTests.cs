using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Tests;

public sealed class SourceStatusTests
{
    [Theory]
    [InlineData(false, true, true, SensorSourceStatus.DriverMissing, false)]
    [InlineData(true, false, true, SensorSourceStatus.DriverMissing, false)]   // installed, but not elevated: values would be fake zeros
    [InlineData(true, true, false, SensorSourceStatus.BoardNotDetected, true)]
    [InlineData(true, true, true, SensorSourceStatus.Ok, true)]
    public void Status_requires_the_driver_and_privileges_and_reports_a_missing_board(
        bool driverInstalled, bool privileged, bool superIoFound, SensorSourceStatus expected, bool driverUsable)
    {
        var result = SourceStatus.Resolve(driverInstalled, privileged, superIoFound);

        result.ShouldBe((expected, driverUsable));
    }
}
