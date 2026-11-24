using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Tests.Fakes;

/// <summary>Sensor source that returns a fixed catalog and the next queued reading on each call.</summary>
internal sealed class ScriptedSensorSource(params float[][] readings) : ISensorSource
{
    public static readonly SensorCatalog Catalog = new(
    [
        new SensorDescriptor("/cpu/temp", "CPU", SensorKind.Temperature, "°C", SensorRole.CpuTemperature),
        new SensorDescriptor("/sio/12v", "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V),
    ]);

    private readonly Queue<float[]> _readings = new(readings);

    public Exception? ReadFailure { get; set; }

    public SensorSourceInfo Open() => new(SensorSourceStatus.Ok, "TEST BOARD", Catalog);

    public IReadOnlyList<float> Read() =>
        ReadFailure is not null ? throw ReadFailure : _readings.Count > 0 ? _readings.Dequeue() : [float.NaN, float.NaN];
}
