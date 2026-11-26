using PcMonitor.Dashboard.Api.Sensors;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class SensorPresentationTests
{
    [Fact]
    public void Disk_sensors_carry_their_disk_and_measure()
    {
        var temperature = SensorPresentation.Describe(new SensorDescriptor("/nvme/0/temperature/0", "Composite Temperature", SensorKind.Temperature, "°C", SensorRole.None));

        temperature.Storage.ShouldBe(new StorageResponse(0, StorageMeasureKind.Temperature));
    }

    [Fact]
    public void Disk_life_below_10_percent_is_out_of_range()
    {
        var life = SensorPresentation.Describe(new SensorDescriptor("/nvme/0/level/20", "Life", SensorKind.Level, "%", SensorRole.None));

        life.Range.ShouldBe(new RangeResponse(10, 100));
    }

    [Fact]
    public void Other_sensors_have_no_storage_measure() =>
        SensorPresentation.Describe(new SensorDescriptor("/ram/load/0", "Memory", SensorKind.Load, "%", SensorRole.MemoryLoad)).Storage.ShouldBeNull();
}
