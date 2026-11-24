using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Diagnosis.Rules;

internal static class SensorLimitation
{
    /// <summary>The rule needs a sensor this board or session did not record.</summary>
    public static Finding Unavailable(SensorRole role) =>
        Finding.Limitation("sensor.unavailable", FindingParameters.Of(("role", role.ToString())));
}
