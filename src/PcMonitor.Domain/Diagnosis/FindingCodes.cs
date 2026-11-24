namespace PcMonitor.Domain.Diagnosis;

/// <summary>
/// Every code a diagnosis can contain. Codes are stable identifiers stored in the database and translated by the
/// dashboard; the catalogue and meaning of each one is in <c>docs/architecture/diagnosis.md</c>.
/// </summary>
public static class FindingCodes
{
    public const string ThermalCritical = "thermal.critical";
    public const string ThermalAtLimit = "thermal.at_limit";
    public const string ThermalNormal = "thermal.normal";
    public const string RailOutOfTolerance = "rail.out_of_tolerance";
    public const string RailDroop = "rail.droop";
    public const string RailInTolerance = "rail.in_tolerance";
    public const string LoadHigh = "load.high";
    public const string LoadIdle = "load.idle";
    public const string RestartQuick = "restart.quick";
    public const string RestartStayedOff = "restart.stayed_off";
    public const string PowerTransition = "power.transition";
    public const string Bugcheck = "bugcheck";
    public const string BugcheckNone = "bugcheck.none";
    public const string WheaHardwareError = "whea.hardware_error";
    public const string WheaPcie = "whea.pcie";
    public const string PowerButtonLongPress = "power_button.long_press";
    public const string PowerButtonPressed = "power_button.pressed";
    public const string BootEarlyFailure = "boot.early_failure";
    public const string TelemetryMissing = "telemetry.missing";
    public const string TelemetryNotAtFailure = "telemetry.not_at_failure";
    public const string TelemetryTooShort = "telemetry.too_short";
    public const string EstimateLowerBound = "estimate.lower_bound";
    public const string SensorUnavailable = "sensor.unavailable";
    public const string RailPrecision = "rail.precision";
    public const string RailMeasuredOnBoard = "rail.measured_on_board";
    public const string WheaMayBePower = "whea.may_be_power";

    /// <summary>All codes, in declaration order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ThermalCritical,
        ThermalAtLimit,
        ThermalNormal,
        RailOutOfTolerance,
        RailDroop,
        RailInTolerance,
        LoadHigh,
        LoadIdle,
        RestartQuick,
        RestartStayedOff,
        PowerTransition,
        Bugcheck,
        BugcheckNone,
        WheaHardwareError,
        WheaPcie,
        PowerButtonLongPress,
        PowerButtonPressed,
        BootEarlyFailure,
        TelemetryMissing,
        TelemetryNotAtFailure,
        TelemetryTooShort,
        EstimateLowerBound,
        SensorUnavailable,
        RailPrecision,
        RailMeasuredOnBoard,
        WheaMayBePower,
    ];
}
