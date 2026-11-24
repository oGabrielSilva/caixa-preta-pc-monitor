using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;
using PcMonitor.Domain.Tests.Diagnosis.Support;

namespace PcMonitor.Domain.Tests.Diagnosis;

/// <summary>
/// Whole-engine scenarios with the real rules, shaped like the incidents this machine produces. They pin the
/// confidence the owner will see; changing a rule that breaks one of them requires bumping DiagnosisEngine.Version.
/// </summary>
public sealed class DiagnosisScenarioTests
{
    private static readonly DiagnosisEngine _engine = new(DiagnosticRules.All);

    private static IncidentDiagnosis Diagnose(EvidenceBuilder builder) => _engine.Diagnose(builder.Build());

    private static int Score(IncidentDiagnosis diagnosis, Hypothesis hypothesis) =>
        diagnosis.Ranking.SingleOrDefault(s => s.Hypothesis == hypothesis)?.Score ?? 0;

    private static EvidenceBuilder WithRailsInTolerance(EvidenceBuilder builder) => builder
        .Constant(SensorRole.Rail12V, 12.19f).Constant(SensorRole.Rail5V, 5f).Constant(SensorRole.Rail3V3, 3.3f);

    [Fact]
    public void Crash_during_hibernation_without_telemetry_leans_to_board_power_delivery_with_low_confidence()
    {
        var diagnosis = Diagnose(new EvidenceBuilder { SleepState = 4, Source = FailureTimeSource.LastAliveEventLog, LastAliveAt = EvidenceBuilder.End });

        (diagnosis.Top, diagnosis.Confidence).ShouldBe((Hypothesis.BoardPowerDelivery, Confidence.Low));
        diagnosis.Limitations.Select(l => l.Code).ShouldContain("telemetry.missing");
    }

    [Fact]
    public void Early_failure_without_telemetry_is_inconclusive_with_the_observation()
    {
        var boot = EvidenceBuilder.End;
        var diagnosis = Diagnose(new EvidenceBuilder { Source = FailureTimeSource.LastAliveEventLog, LastAliveAt = boot }
            .Event("EventLog", 6005, boot));

        diagnosis.Confidence.ShouldBe(Confidence.Inconclusive);
        diagnosis.Findings.Select(f => f.Code).ShouldContain("boot.early_failure");
    }

    [Fact]
    public void Power_supply_trip_during_a_stress_test_is_not_blamed_on_heat()
    {
        // 100 % load, the CPU climbing to 93 °C (normal for this CPU under load), rails fine at 1 Hz, then it dies.
        float[] temperatures = [.. Enumerable.Repeat(55f, 90), .. Enumerable.Range(0, 30).Select(i => 55f + (i * 38f / 29f))];
        var diagnosis = Diagnose(WithRailsInTolerance(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddMinutes(30) })
            .Series(SensorRole.CpuTemperature, temperatures)
            .Constant(SensorRole.CpuLoad, 100f));

        diagnosis.Top.ShouldNotBe(Hypothesis.Thermal);
        diagnosis.Confidence.ShouldNotBe(Confidence.High);
    }

    [Fact]
    public void Peak_temperature_long_before_the_failure_is_not_thermal_evidence()
    {
        // A benchmark peaked at 99 °C, then the machine idled for a minute and died at 45 °C.
        float[] temperatures = [.. Enumerable.Repeat(99f, 30), .. Enumerable.Repeat(45f, 90)];
        var diagnosis = Diagnose(new EvidenceBuilder().Series(SensorRole.CpuTemperature, temperatures));

        Score(diagnosis, Hypothesis.Thermal).ShouldBeLessThanOrEqualTo(0);
    }

    [Fact]
    public void Crash_while_hibernating_with_telemetry_from_before_sleep_is_not_more_confident_than_without()
    {
        var withoutTelemetry = Diagnose(new EvidenceBuilder { SleepState = 4, Source = FailureTimeSource.LastFrameBeforeSleep });
        var withTelemetry = Diagnose(WithRailsInTolerance(new EvidenceBuilder { SleepState = 4, Source = FailureTimeSource.LastFrameBeforeSleep })
            .Constant(SensorRole.CpuLoad, 2f).Constant(SensorRole.CpuTemperature, 40f));

        withTelemetry.Ranking.ShouldBe(withoutTelemetry.Ranking);
        withTelemetry.Confidence.ShouldBe(withoutTelemetry.Confidence);
        withTelemetry.Limitations.Select(l => l.Code).ShouldContain("telemetry.not_at_failure");
    }

    [Fact]
    public void A_single_glitched_rail_sample_does_not_make_a_confident_diagnosis()
    {
        float[] rail = [.. Enumerable.Repeat(12.19f, 119), 11.35f];
        var diagnosis = Diagnose(new EvidenceBuilder()
            .Series(SensorRole.Rail12V, rail).Constant(SensorRole.Rail5V, 5f).Constant(SensorRole.Rail3V3, 3.3f)
            .Constant(SensorRole.CpuLoad, 90f).Constant(SensorRole.CpuTemperature, 60f));

        diagnosis.Confidence.ShouldNotBe(Confidence.High);
        Score(diagnosis, Hypothesis.PowerSupply).ShouldBeLessThan(3);
    }

    [Fact]
    public void Rail_out_of_tolerance_does_not_favour_the_supply_over_the_board()
    {
        float[] rail = [.. Enumerable.Repeat(12.19f, 100), .. Enumerable.Repeat(11.2f, 20)];
        var diagnosis = Diagnose(new EvidenceBuilder()
            .Series(SensorRole.Rail12V, rail).Constant(SensorRole.Rail5V, 5f).Constant(SensorRole.Rail3V3, 3.3f)
            .Constant(SensorRole.CpuLoad, 45f));

        Score(diagnosis, Hypothesis.PowerSupply).ShouldBe(Score(diagnosis, Hypothesis.BoardPowerDelivery));
        diagnosis.Limitations.Select(l => l.Code).ShouldContain("rail.measured_on_board");
    }

    [Fact]
    public void Quick_return_after_a_crash_is_an_observation_not_evidence_against_the_supply()
    {
        var diagnosis = Diagnose(new EvidenceBuilder { LoggedAt = EvidenceBuilder.End.AddSeconds(90) }.WithTelemetry());

        Score(diagnosis, Hypothesis.PowerSupply).ShouldBe(0);
        diagnosis.Findings.Single(f => f.Code == "restart.quick").Kind.ShouldBe(FindingKind.Observation);
    }
}
