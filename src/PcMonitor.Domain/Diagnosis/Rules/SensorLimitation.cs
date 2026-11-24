using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Diagnosis.Rules;

internal static class SensorLimitation
{
    /// <summary>The rule needs a sensor this board or session did not record.</summary>
    public static Finding Unavailable(SensorRole role) =>
        Finding.Limitation(FindingCodes.SensorUnavailable, FindingParameters.Of(("role", role.ToString())));

    /// <summary>The sensor was recorded, but not long enough before the failure for the rule to conclude.</summary>
    public static Finding TooShort(SensorRole role) =>
        Finding.Limitation(FindingCodes.TelemetryTooShort, FindingParameters.Of(("role", role.ToString())));
}
