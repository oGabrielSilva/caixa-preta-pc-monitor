using System.ServiceProcess;
using LibreHardwareMonitor.Hardware;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

namespace PcMonitor.Infrastructure.Sensors.Lhm;

/// <summary>
/// <see cref="ISensorSource"/> on LibreHardwareMonitor: CPU, motherboard (Super I/O) and memory.
/// Requires administrator rights and the PawnIO driver for anything other than load sensors (ADR 0010).
/// </summary>
public sealed class LhmSensorSource : ISensorSource, IDisposable
{
    private const string DriverServiceName = "PawnIO";

    private readonly Computer _computer = new() { IsCpuEnabled = true, IsMotherboardEnabled = true, IsMemoryEnabled = true };
    private readonly UpdateVisitor _updater = new();
    private IReadOnlyList<SelectedSensor> _selected = [];

    public SensorSourceInfo Open()
    {
        _computer.Open();
        _computer.Accept(_updater);

        var hardware = Flatten(_computer.Hardware).ToList();
        var (status, driverUsable) = SourceStatus.Resolve(
            driverInstalled: LibreHardwareMonitor.PawnIo.PawnIo.IsInstalled,
            driverRunning: IsDriverServiceRunning(),
            privileged: Environment.IsPrivilegedProcess,
            superIoFound: hardware.Any(h => h.HardwareType == HardwareType.SuperIO));
        var boardProduct = _computer.SMBios?.Board?.ProductName?.Trim();
        var profile = BoardProfileCatalog.FindFor(boardProduct);

        _selected = SensorSelection.Select(hardware.SelectMany(h => h.Sensors), profile, driverUsable);

        return new SensorSourceInfo(status, boardProduct, new SensorCatalog(_selected.Select(s => s.Descriptor)));
    }

    public IReadOnlyList<float> Read()
    {
        _computer.Accept(_updater);
        var values = new float[_selected.Count];
        for (var i = 0; i < values.Length; i++)
        {
            var selected = _selected[i];
            values[i] = selected.Sensor.Value is { } value ? value * selected.Multiplier : float.NaN;
        }

        return values;
    }

    public void Dispose() => _computer.Close();

    /// <summary>PawnIO is a demand-start kernel driver; after <c>Open()</c> its service must be running.</summary>
    private static bool IsDriverServiceRunning()
    {
        try
        {
            using var driver = new ServiceController(DriverServiceName);
            return driver.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return false; // Service not installed.
        }
    }

    private static IEnumerable<IHardware> Flatten(IEnumerable<IHardware> hardware) =>
        hardware.SelectMany(h => Flatten(h.SubHardware).Prepend(h));

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
