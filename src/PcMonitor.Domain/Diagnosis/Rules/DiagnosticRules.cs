namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>Every rule the engine runs. A test fails if a rule exists but is not listed here.</summary>
public static class DiagnosticRules
{
    public static IReadOnlyList<IDiagnosticRule> All { get; } =
    [
        new ThermalRule(),
        new RailToleranceRule(),
        new RailDroopRule(),
        new LoadAtFailureRule(),
        new RestartGapRule(),
        new SleepTransitionRule(),
        new BugcheckRule(),
        new WheaRule(),
        new PowerButtonRule(),
        new BootTimingRule(),
    ];
}
