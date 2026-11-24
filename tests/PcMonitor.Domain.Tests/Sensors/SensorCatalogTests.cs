using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Tests.Sensors;

public sealed class SensorCatalogTests
{
    private static SensorDescriptor Sensor(string key, SensorRole role = SensorRole.None) =>
        new(key, key, SensorKind.Voltage, "V", role);

    [Fact]
    public void Catalog_keeps_the_given_order()
    {
        var catalog = new SensorCatalog([Sensor("/c"), Sensor("/a"), Sensor("/b")]);

        catalog.Sensors.Select(s => s.StableKey).ShouldBe(["/c", "/a", "/b"]);
        catalog[1].StableKey.ShouldBe("/a");
        catalog.Count.ShouldBe(3);
    }

    [Fact]
    public void Catalog_rejects_duplicate_stable_keys() =>
        Should.Throw<ArgumentException>(() => new SensorCatalog([Sensor("/a"), Sensor("/a")]));

    [Fact]
    public void Catalog_rejects_a_role_assigned_twice() =>
        Should.Throw<ArgumentException>(() =>
            new SensorCatalog([Sensor("/a", SensorRole.Rail12V), Sensor("/b", SensorRole.Rail12V)]));

    [Fact]
    public void Catalog_allows_many_sensors_without_a_role() =>
        new SensorCatalog([Sensor("/a"), Sensor("/b")]).Count.ShouldBe(2);

    [Fact]
    public void OrdinalOf_finds_the_sensor_with_the_role()
    {
        var catalog = new SensorCatalog([Sensor("/a"), Sensor("/b"), Sensor("/c", SensorRole.Rail12V)]);

        catalog.OrdinalOf(SensorRole.Rail12V).ShouldBe(2);
        catalog.OrdinalOf(SensorRole.CpuTemperature).ShouldBeNull();
    }
}
