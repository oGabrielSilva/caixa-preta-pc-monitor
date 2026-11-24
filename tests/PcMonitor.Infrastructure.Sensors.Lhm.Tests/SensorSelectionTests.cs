using LibreHardwareMonitor.Hardware;
using NSubstitute;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Tests;

public sealed class SensorSelectionTests
{
    private static readonly BoardProfile _profile = BoardProfileCatalog.FindFor("TUF GAMING A620M-PLUS WIFI")!;

    private static ISensor Sensor(string key, SensorType type, string name = "raw")
    {
        var sensor = Substitute.For<ISensor>();
        sensor.Identifier.Returns(new Identifier(key.TrimStart('/').Split('/')));
        sensor.SensorType.Returns(type);
        sensor.Name.Returns(name);
        return sensor;
    }

    [Fact]
    public void Selection_keeps_only_temperature_voltage_power_load_and_fan()
    {
        ISensor[] sensors =
        [
            Sensor("/x/temperature/0", SensorType.Temperature), Sensor("/x/voltage/0", SensorType.Voltage),
            Sensor("/x/power/0", SensorType.Power), Sensor("/x/load/0", SensorType.Load), Sensor("/x/fan/0", SensorType.Fan),
            Sensor("/x/clock/0", SensorType.Clock), Sensor("/x/control/0", SensorType.Control),
            Sensor("/x/data/0", SensorType.Data), Sensor("/x/timing/0", SensorType.Timing),
        ];

        SensorSelection.Select(sensors, profile: null, driverAvailable: true)
            .Select(s => s.Descriptor.Kind)
            .ShouldBe([SensorKind.Fan, SensorKind.Load, SensorKind.Power, SensorKind.Temperature, SensorKind.Voltage],
                ignoreOrder: true);
    }

    [Fact]
    public void Selection_without_driver_keeps_only_load_sensors()
    {
        ISensor[] sensors = [Sensor("/amdcpu/0/temperature/2", SensorType.Temperature), Sensor("/amdcpu/0/load/0", SensorType.Load)];

        SensorSelection.Select(sensors, _profile, driverAvailable: false)
            .ShouldHaveSingleItem().Descriptor.StableKey.ShouldBe("/amdcpu/0/load/0");
    }

    [Fact]
    public void Selection_applies_profile_names_roles_and_multipliers()
    {
        var selected = SensorSelection.Select([Sensor("/lpc/nct6799d/0/voltage/4", SensorType.Voltage, "Voltage #5")], _profile, true)
            .ShouldHaveSingleItem();

        selected.Descriptor.ShouldBe(new SensorDescriptor("/lpc/nct6799d/0/voltage/4", "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V));
        selected.Multiplier.ShouldBe(12);
    }

    [Fact]
    public void Selection_keeps_the_library_name_and_no_role_for_sensors_outside_the_profile()
    {
        var selected = SensorSelection.Select([Sensor("/lpc/nct6799d/0/fan/0", SensorType.Fan, "Fan #1")], _profile, true)
            .ShouldHaveSingleItem();

        selected.Descriptor.ShouldBe(new SensorDescriptor("/lpc/nct6799d/0/fan/0", "Fan #1", SensorKind.Fan, "RPM", SensorRole.None));
        selected.Multiplier.ShouldBe(1);
    }

    [Theory]
    [InlineData("/amdcpu/0/voltage/0")]
    [InlineData("/amdcpu/0/power/3")]
    [InlineData("/amdcpu/0/load/5")]
    [InlineData("/memory/dimm/1/temperature/3")]
    public void Selection_drops_excluded_keys(string key) =>
        SensorSelection.Select([Sensor(key, key.Contains("voltage") ? SensorType.Voltage : key.Contains("power") ? SensorType.Power
            : key.Contains("load") ? SensorType.Load : SensorType.Temperature)], _profile, true).ShouldBeEmpty();

    [Fact]
    public void Selection_orders_sensors_by_stable_key()
    {
        ISensor[] sensors = [Sensor("/b/load/0", SensorType.Load), Sensor("/a/load/0", SensorType.Load)];

        SensorSelection.Select(sensors, null, true).Select(s => s.Descriptor.StableKey).ShouldBe(["/a/load/0", "/b/load/0"]);
    }
}
