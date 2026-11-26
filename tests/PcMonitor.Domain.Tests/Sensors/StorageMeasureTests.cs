using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Tests.Sensors;

public sealed class StorageMeasureTests
{
    [Theory]
    [InlineData("/nvme/0/temperature/0", 0, StorageMeasureKind.Temperature)]
    [InlineData("/nvme/0/temperature/10", 0, StorageMeasureKind.WarningTemperature)]
    [InlineData("/nvme/0/temperature/11", 0, StorageMeasureKind.CriticalTemperature)]
    [InlineData("/nvme/1/load/30", 1, StorageMeasureKind.UsedSpace)]
    [InlineData("/nvme/2/level/20", 2, StorageMeasureKind.Life)]
    public void Nvme_sensors_are_recognised_by_their_key(string key, int disk, StorageMeasureKind kind) =>
        StorageMeasure.Of(key).ShouldBe(new StorageMeasure(disk, kind));

    [Theory]
    [InlineData("/nvme/0/temperature/1")]
    [InlineData("/nvme/0/load/53")]
    [InlineData("/amdcpu/0/temperature/2")]
    [InlineData("/ram/load/0")]
    public void Other_sensors_are_not_storage_measures(string key) => StorageMeasure.Of(key).ShouldBeNull();
}
