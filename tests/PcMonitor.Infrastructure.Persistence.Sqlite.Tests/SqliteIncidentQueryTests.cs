using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;
using PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;
using PcMonitor.Tests.Support;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Tests;

public sealed class SqliteIncidentQueryTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    private readonly TempDatabase _temp = TempDatabase.Migrated();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 24, 18, 0, 0, TimeSpan.Zero));
    private readonly DatabaseSeed _seed;
    private readonly SqliteIncidentQuery _query;

    public SqliteIncidentQueryTests()
    {
        _seed = new DatabaseSeed(_temp.Database);
        _query = new SqliteIncidentQuery(_temp.Database);
    }

    public void Dispose()
    {
        _seed.Dispose();
        _temp.Dispose();
    }

    /// <summary>An idle recording that ends abruptly, the incident that ended it and its diagnosis.</summary>
    private long CrashWithTelemetry()
    {
        var (session, segment) = _seed.Recording(_t0, 180);
        var end = _t0.AddSeconds(179);
        var id = _seed.Incident(41, end.AddMinutes(2), FailureTimeSource.LastFrame, end, session, segment, end, 0,
            new SystemEvent("EventLog", 6008, 50, end.AddMinutes(2).AddSeconds(5), 2, new Dictionary<string, string>()),
            DatabaseSeed.PrivateShutdownEvent(51, end.AddMinutes(-1)));
        _seed.Diagnose(id, _time);
        return id;
    }

    [Fact]
    public void List_is_newest_first_with_diagnosis_fields()
    {
        var older = _seed.Incident(1, _t0.AddDays(-1), sleepState: 4);
        _seed.Diagnose(older, _time);
        var newer = _seed.Incident(2, _t0);

        var list = _query.List();

        list.Select(i => i.Id).ShouldBe([newer, older]);
        list[1].TopHypothesis.ShouldBe(Hypothesis.BoardPowerDelivery);
        list[1].Confidence.ShouldBe(Confidence.Low);
        list[1].SleepState.ShouldBe(4);
        list[1].HasTelemetry.ShouldBeFalse();
    }

    [Fact]
    public void Undiagnosed_incident_has_null_confidence()
    {
        _seed.Incident(1, _t0);

        var summary = _query.List().ShouldHaveSingleItem();

        (summary.TopHypothesis, summary.Confidence).ShouldBe((null, null));
        summary.ObservationCodes.ShouldBeEmpty();
    }

    [Fact]
    public void Observation_codes_come_from_the_diagnosis()
    {
        var id = _seed.Incident(1, _t0.AddHours(2), FailureTimeSource.LastAliveEventLog, _t0, events:
            new SystemEvent("EventLog", 6005, 9, _t0, 4, new Dictionary<string, string>()));
        _seed.Diagnose(id, _time);

        _query.List().ShouldHaveSingleItem().ObservationCodes.ShouldContain(FindingCodes.BootEarlyFailure);
    }

    [Fact]
    public void Find_returns_events_without_any_event_data()
    {
        var detail = _query.Find(CrashWithTelemetry()).ShouldNotBeNull();

        detail.Events.Select(e => (e.Provider, e.EventId)).ShouldBe([("User32", 1074), ("EventLog", 6008)]);
        var json = JsonSerializer.Serialize(detail with { Telemetry = null });
        json.ShouldNotContain(DatabaseSeed.ComputerName);
        json.ShouldNotContain(DatabaseSeed.UserName);
    }

    [Fact]
    public void Find_returns_the_stored_diagnosis()
    {
        var detail = _query.Find(CrashWithTelemetry()).ShouldNotBeNull();

        var diagnosis = detail.Diagnosis.ShouldNotBeNull();
        diagnosis.EngineVersion.ShouldBe(DiagnosisEngine.Version);
        diagnosis.DiagnosedAt.ShouldBe(_time.GetUtcNow());
        diagnosis.Findings.ShouldContain(f => f.Code == FindingCodes.LoadIdle && f.Hypothesis == Hypothesis.BoardPowerDelivery && f.Supports);
        diagnosis.Limitations.ShouldContain(f => f.Code == FindingCodes.RailPrecision);
        diagnosis.Ranking[0].Hypothesis.ShouldBe(detail.Summary.TopHypothesis!.Value);
    }

    [Fact]
    public void Find_includes_the_same_telemetry_window_the_diagnosis_used()
    {
        var id = CrashWithTelemetry();

        var telemetry = _query.Find(id).ShouldNotBeNull().Telemetry.ShouldNotBeNull();
        var expected = new SqliteDiagnosisStore(_temp.Database, _time).Load(id).Telemetry.ShouldNotBeNull();

        telemetry.End.ShouldBe(expected.End);
        telemetry.Series(SensorRole.Rail12V).ShouldBe(expected.Series(SensorRole.Rail12V));
        telemetry.Series(SensorRole.Rail12V).Count.ShouldBe(121);
    }

    [Fact]
    public void Find_without_telemetry_has_null_window()
    {
        var detail = _query.Find(_seed.Incident(1, _t0)).ShouldNotBeNull();

        (detail.Telemetry, detail.Diagnosis, detail.TelemetryEndAt).ShouldBe((null, null, null));
    }

    [Fact]
    public void Find_unknown_id_returns_null() => _query.Find(404).ShouldBeNull();
}
