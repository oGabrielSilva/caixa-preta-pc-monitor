using Dapper;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;
using PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteDiagnosisStoreTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    private static readonly SensorCatalog _catalog = new(
    [
        new SensorDescriptor("/cpu/temp", "CPU", SensorKind.Temperature, "°C", SensorRole.CpuTemperature),
        new SensorDescriptor("/fan/1", "Fan #1", SensorKind.Fan, "RPM", SensorRole.None),
        new SensorDescriptor("/sio/12v", "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V),
    ]);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly SqliteMonitoringStore _monitoring;
    private readonly SqliteIncidentStore _incidents;
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 25, 9, 0, 0, TimeSpan.Zero));
    private readonly SqliteDiagnosisStore _store;

    public SqliteDiagnosisStoreTests()
    {
        _monitoring = new SqliteMonitoringStore(_temp.Database);
        _incidents = new SqliteIncidentStore(_temp.Database);
        _store = new SqliteDiagnosisStore(_temp.Database, _time);
    }

    public void Dispose()
    {
        _monitoring.Dispose();
        _temp.Dispose();
    }

    private static UnexpectedShutdown Crash(long record, DateTimeOffset loggedAt) =>
        new(record, loggedAt, 0, SleepState: 4, null, false, loggedAt.AddMinutes(-5), new Dictionary<string, string> { ["SleepInProgress"] = "4" });

    private static SystemEvent Event(long record, DateTimeOffset at) =>
        new("User32", 1074, record, at, 4, new Dictionary<string, string>());

    private void SaveUnlinked(long record, DateTimeOffset loggedAt, params SystemEvent[] related) =>
        _incidents.SaveIncident(new Incident(Crash(record, loggedAt), null, FailureTimeSource.Unknown, null, null, null, related), null);

    private static IncidentDiagnosis Diagnosis(int version) =>
        new(version, [new HypothesisScore(Hypothesis.PowerSupply, 2)], Confidence.Low,
            [Finding.For("rail.droop", Hypothesis.PowerSupply, EvidenceStrength.Moderate, new Dictionary<string, string> { ["rail"] = "12V" })],
            [Finding.Limitation("rail.precision")]);

    [Fact]
    public void Incident_without_diagnosis_needs_one()
    {
        SaveUnlinked(1, _t0);

        var input = _store.Load(_store.IncidentsNeedingDiagnosis(DiagnosisEngine.Version).ShouldHaveSingleItem());

        input.Incident.Shutdown.RecordId.ShouldBe(1);
        input.Incident.Shutdown.SleepState.ShouldBe(4);
        input.Incident.Shutdown.Data["SleepInProgress"].ShouldBe("4");
        input.Telemetry.ShouldBeNull();
    }

    [Fact]
    public void Diagnosis_of_the_current_version_is_not_redone_but_an_older_one_is()
    {
        SaveUnlinked(1, _t0);
        var id = _store.IncidentsNeedingDiagnosis(2).Single();
        _store.Save(id, Diagnosis(version: 2));

        _store.IncidentsNeedingDiagnosis(2).ShouldBeEmpty();
        _store.IncidentsNeedingDiagnosis(3).ShouldHaveSingleItem();
    }

    [Fact]
    public void Save_replaces_an_older_diagnosis()
    {
        SaveUnlinked(1, _t0);
        var id = _store.IncidentsNeedingDiagnosis(1).Single();
        _store.Save(id, Diagnosis(version: 1));

        _store.Save(id, Diagnosis(version: 2));

        using var connection = _temp.Database.OpenReader();
        connection.QuerySingle<(long, string, string, long)>(
            "SELECT engine_version, top_hypothesis, confidence, diagnosed_at_ms FROM diagnosis")
            .ShouldBe((2L, "PowerSupply", "Low", _time.GetUtcNow().ToUnixTimeMilliseconds()));
        connection.QuerySingle<string>("SELECT findings_json FROM diagnosis").ShouldContain("rail.droop");
        connection.QuerySingle<string>("SELECT limitations_json FROM diagnosis").ShouldContain("rail.precision");
    }

    [Fact]
    public void Stored_json_has_the_shape_the_dashboard_reads()
    {
        SaveUnlinked(1, _t0);
        var id = _store.IncidentsNeedingDiagnosis(1).Single();

        _store.Save(id, new IncidentDiagnosis(1, [new HypothesisScore(Hypothesis.BoardPowerDelivery, 2)], Confidence.Low,
            [
                Finding.For("power.transition", Hypothesis.BoardPowerDelivery, EvidenceStrength.Moderate, new Dictionary<string, string> { ["state"] = "4" }),
                Finding.Observation("boot.early_failure"),
            ],
            [Finding.Limitation("telemetry.missing")]));

        using var connection = _temp.Database.OpenReader();
        var (ranking, findings, limitations) = connection.QuerySingle<(string, string, string)>(
            "SELECT ranking_json, findings_json, limitations_json FROM diagnosis");
        ranking.ShouldBe("""[{"hypothesis":"BoardPowerDelivery","score":2}]""");
        findings.ShouldBe("""[{"code":"power.transition","kind":"Evidence","hypothesis":"BoardPowerDelivery","supports":true,"strength":"Moderate","parameters":{"state":"4"}},{"code":"boot.early_failure","kind":"Observation","parameters":{}}]""");
        limitations.ShouldBe("""[{"code":"telemetry.missing","kind":"Limitation","parameters":{}}]""");
    }

    [Fact]
    public void Telemetry_window_has_the_last_120_seconds_by_role_across_segments()
    {
        var session = _monitoring.StartSession(_t0, _t0, new SensorSourceInfo(SensorSourceStatus.Ok, null, _catalog));
        var first = _monitoring.StartSegment(session, _t0, _catalog);
        _monitoring.AppendFrame(first, new Frame(_t0, [40f, 1000f, 12.2f]));                 // 200 s before the end: outside
        _monitoring.AppendFrame(first, new Frame(_t0.AddSeconds(100), [50f, 1000f, 12.1f]));
        _monitoring.EndSegment(first, _t0.AddSeconds(101), EndReason.Gap);
        var second = _monitoring.StartSegment(session, _t0.AddSeconds(150), _catalog);
        _monitoring.AppendFrame(second, new Frame(_t0.AddSeconds(200), [80f, 1000f, float.NaN]));
        var end = _t0.AddSeconds(200);
        _incidents.SaveIncident(new Incident(Crash(1, end.AddMinutes(2)), end, FailureTimeSource.LastFrame, session, second, end, []), null);

        var telemetry = _store.Load(_store.IncidentsNeedingDiagnosis(DiagnosisEngine.Version).Single()).Telemetry.ShouldNotBeNull();

        telemetry.End.ShouldBe(end);
        telemetry.Series(SensorRole.CpuTemperature).Select(p => p.Value).ShouldBe([50f, 80f]);
        telemetry.Series(SensorRole.Rail12V).Select(p => p.Value).ShouldBe([12.1f]);
        telemetry.Has(SensorRole.None).ShouldBeFalse();
    }

    [Fact]
    public void Related_events_outside_the_neighbouring_incidents_are_left_out()
    {
        // Incident 2's stored events (from an old, wide window) include events of incidents 1 and 3.
        SaveUnlinked(1, _t0);
        SaveUnlinked(2, _t0.AddMinutes(10), Event(100, _t0.AddMinutes(-1)), Event(101, _t0.AddMinutes(5)), Event(102, _t0.AddMinutes(15)));
        SaveUnlinked(3, _t0.AddMinutes(12));

        var second = LoadByRecord(2);

        second.Incident.RelatedEvents.Select(e => e.RecordId).ShouldBe([101L]);
    }

    [Fact]
    public void Boot_events_right_after_a_crash_belong_only_to_that_crash()
    {
        // Both incidents carry the event logged 30 s after crash 1 (old wide windows).
        SaveUnlinked(1, _t0, Event(100, _t0.AddSeconds(30)));
        SaveUnlinked(2, _t0.AddMinutes(10), Event(100, _t0.AddSeconds(30)), Event(101, _t0.AddMinutes(5)));

        LoadByRecord(1).Incident.RelatedEvents.Select(e => e.RecordId).ShouldBe([100L]);
        LoadByRecord(2).Incident.RelatedEvents.Select(e => e.RecordId).ShouldBe([101L]);
    }

    [Fact]
    public void Neighbours_are_found_by_time_so_a_cleared_event_log_does_not_lose_events()
    {
        // After the System log was cleared, a newer crash got a smaller record id than an older one.
        SaveUnlinked(60, _t0);
        SaveUnlinked(50, _t0.AddDays(1), Event(7, _t0.AddDays(1).AddSeconds(10)));

        LoadByRecord(50).Incident.RelatedEvents.Select(e => e.RecordId).ShouldBe([7L]);
    }

    private IncidentEvidence LoadByRecord(long record) =>
        _store.IncidentsNeedingDiagnosis(DiagnosisEngine.Version).Select(_store.Load).Single(i => i.Incident.Shutdown.RecordId == record);
}
