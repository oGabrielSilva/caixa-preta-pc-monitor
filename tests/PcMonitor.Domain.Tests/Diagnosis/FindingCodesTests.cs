using System.Reflection;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis;

public sealed class FindingCodesTests
{
    private static readonly DiagnosisEngine _engine = new(DiagnosticRules.All);

    [Fact]
    public void All_lists_every_constant_once()
    {
        var constants = typeof(FindingCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        FindingCodes.All.ShouldBe(constants);
        FindingCodes.All.Distinct().Count().ShouldBe(FindingCodes.All.Count);
    }

    [Fact]
    public void Codes_match_the_documented_catalogue() =>
        FindingCodes.All.ShouldBe(
        [
            "thermal.critical", "thermal.at_limit", "thermal.normal",
            "rail.out_of_tolerance", "rail.droop", "rail.in_tolerance",
            "load.high", "load.idle",
            "restart.quick", "restart.stayed_off",
            "power.transition",
            "bugcheck", "bugcheck.none",
            "whea.hardware_error", "whea.pcie",
            "power_button.long_press", "power_button.pressed",
            "boot.early_failure",
            "telemetry.missing", "telemetry.not_at_failure", "telemetry.too_short",
            "estimate.lower_bound", "sensor.unavailable",
            "rail.precision", "rail.measured_on_board", "whea.may_be_power",
        ], ignoreOrder: true);

    public static TheoryData<string> Scenarios() =>
        ["hot", "rail_out", "idle_droop", "busy", "sleep", "bugcheck", "whea", "no_telemetry", "button", "early_boot", "short"];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_code_a_rule_emits_is_in_the_catalogue(string scenario)
    {
        var diagnosis = _engine.Diagnose(Build(scenario).Build());

        diagnosis.Findings.Concat(diagnosis.Limitations).Select(f => f.Code).ShouldAllBe(code => FindingCodes.All.Contains(code));
    }

    private static EvidenceBuilder Build(string scenario)
    {
        var end = EvidenceBuilder.End;
        return scenario switch
        {
            "hot" => new EvidenceBuilder().Constant(SensorRole.CpuTemperature, 101f).Constant(SensorRole.CpuLoad, 90f),
            "rail_out" => new EvidenceBuilder().Constant(SensorRole.Rail12V, 11.2f).Constant(SensorRole.Rail5V, 5f).Constant(SensorRole.Rail3V3, 3.3f),
            "idle_droop" => new EvidenceBuilder()
                .Series(SensorRole.Rail12V, [.. Enumerable.Repeat(12.19f, 115), .. Enumerable.Repeat(11.9f, 5)])
                .Constant(SensorRole.CpuLoad, 3f).Constant(SensorRole.CpuTemperature, 40f),
            "busy" => new EvidenceBuilder().Constant(SensorRole.CpuTemperature, 96f).Constant(SensorRole.CpuLoad, 100f),
            "sleep" => new EvidenceBuilder { SleepState = 4, Source = FailureTimeSource.LastFrameBeforeSleep },
            "bugcheck" => new EvidenceBuilder { BugcheckCode = 0x9F, Source = FailureTimeSource.LastAliveEventLog, LastAliveAt = end },
            "whea" => new EvidenceBuilder { BugcheckCode = 0x124 }.Event("Microsoft-Windows-WHEA-Logger", 17, end).Event("Microsoft-Windows-WHEA-Logger", 18, end),
            "no_telemetry" => new EvidenceBuilder { Source = FailureTimeSource.Unknown },
            "button" => new EvidenceBuilder { LongPowerButtonPress = true, PowerButtonPressedAt = end },
            "early_boot" => new EvidenceBuilder { Source = FailureTimeSource.LastAliveEventLog, LastAliveAt = end, LoggedAt = end.AddHours(2) }
                .Event("EventLog", 6005, end),
            "short" => new EvidenceBuilder().Constant(SensorRole.CpuTemperature, 50f, count: 2),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
    }
}
