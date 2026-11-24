using System.Net;
using PcMonitor.Dashboard.Tests.Support;
using PcMonitor.Domain.Incidents;
using PcMonitor.Tests.Support;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class IncidentEndpointsTests : IDisposable
{
    private static readonly DateTimeOffset _t0 = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    private readonly SeededDashboard _dashboard = new();

    public void Dispose() => _dashboard.Dispose();

    /// <summary>An idle recording that ends abruptly, with a NaN on the last +5 V reading, and the incident it ended.</summary>
    private long CrashWithTelemetry(FailureTimeSource source = FailureTimeSource.LastFrame)
    {
        var seed = _dashboard.Seed;
        var session = seed.StartSession(_t0);
        var segment = seed.StartSegment(session, _t0);
        seed.Frames(segment, _t0, 180, s => s == 179 ? [45f, 6f, 20f, 12.19f, float.NaN, 3.3f, 2000f] : DatabaseSeed.Idle(s));
        var end = _t0.AddSeconds(179);
        var id = seed.Incident(41, end.AddMinutes(2), source, end, session, segment, end, 0,
            new SystemEvent("EventLog", 6008, 50, end.AddMinutes(2).AddSeconds(5), 2, new Dictionary<string, string>()),
            new SystemEvent("EventLog", 6005, 52, end.AddMinutes(2).AddSeconds(6), 4, new Dictionary<string, string>()),
            DatabaseSeed.PrivateShutdownEvent(51, end.AddMinutes(-1)));
        seed.Diagnose(id, _dashboard.Time);
        return id;
    }

    [Fact]
    public async Task List_returns_incidents_newest_first()
    {
        var older = _dashboard.Seed.Incident(1, _t0.AddDays(-1), sleepState: 4);
        _dashboard.Seed.Diagnose(older, _dashboard.Time);
        var newer = _dashboard.Seed.Incident(2, _t0);

        var list = await _dashboard.Get("/api/incidents");

        list.EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ShouldBe([newer, older]);
        var first = list[1];
        first.GetProperty("topHypothesis").GetString().ShouldBe("BoardPowerDelivery");
        first.GetProperty("confidence").GetString().ShouldBe("Low");
        first.GetProperty("sleepState").GetInt32().ShouldBe(4);
        first.GetProperty("timeSource").GetString().ShouldBe("Unknown");
        list[0].GetProperty("confidence").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Detail_returns_diagnosis_events_and_telemetry()
    {
        var detail = await _dashboard.Get($"/api/incidents/{CrashWithTelemetry()}");

        detail.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("eventId").GetInt32()).ShouldBe([1074, 6008]);
        var diagnosis = detail.GetProperty("diagnosis");
        diagnosis.GetProperty("ranking")[0].GetProperty("hypothesis").GetString().ShouldBe("BoardPowerDelivery");
        var idle = diagnosis.GetProperty("findings").EnumerateArray().First(f => f.GetProperty("code").GetString() == "load.idle");
        idle.GetProperty("supports").GetBoolean().ShouldBeTrue();
        idle.GetProperty("strength").GetString().ShouldBe("Weak");
        idle.GetProperty("parameters").GetProperty("avg").GetString().ShouldNotBeNullOrEmpty();
        diagnosis.GetProperty("limitations").EnumerateArray().Select(l => l.GetProperty("code").GetString()).ShouldContain("rail.precision");

        var telemetry = detail.GetProperty("telemetry");
        telemetry.GetProperty("atFailure").GetBoolean().ShouldBeTrue();
        var rail = telemetry.GetProperty("series").EnumerateArray().Single(s => s.GetProperty("role").GetString() == "Rail12V");
        rail.GetProperty("unit").GetString().ShouldBe("V");
        rail.GetProperty("range").GetProperty("min").GetSingle().ShouldBe(11.4f);
        rail.GetProperty("range").GetProperty("max").GetSingle().ShouldBe(12.6f);
        rail.GetProperty("points").GetArrayLength().ShouldBe(121);
        var cpu = telemetry.GetProperty("series").EnumerateArray().Single(s => s.GetProperty("role").GetString() == "CpuTemperature");
        (cpu.GetProperty("limit").GetSingle(), cpu.GetProperty("critical").GetSingle()).ShouldBe((95f, 100f));
    }

    [Fact]
    public async Task Detail_never_contains_event_data()
    {
        using var response = await _dashboard.Client().GetAsync(new Uri($"/api/incidents/{CrashWithTelemetry()}", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.ShouldNotContain(DatabaseSeed.ComputerName);
        body.ShouldNotContain(DatabaseSeed.UserName);
    }

    [Fact]
    public async Task Detail_of_an_undiagnosed_incident_without_telemetry_has_nulls()
    {
        var detail = await _dashboard.Get($"/api/incidents/{_dashboard.Seed.Incident(1, _t0)}");

        detail.GetProperty("diagnosis").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
        detail.GetProperty("telemetry").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Telemetry_that_ends_before_the_failure_is_flagged()
    {
        var detail = await _dashboard.Get($"/api/incidents/{CrashWithTelemetry(FailureTimeSource.LastFrameBeforeSleep)}");

        detail.GetProperty("telemetry").GetProperty("atFailure").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Missing_values_are_left_out_of_the_series()
    {
        var detail = await _dashboard.Get($"/api/incidents/{CrashWithTelemetry()}");

        var rail5 = detail.GetProperty("telemetry").GetProperty("series").EnumerateArray().Single(s => s.GetProperty("role").GetString() == "Rail5V");
        rail5.GetProperty("points").GetArrayLength().ShouldBe(120);
    }

    [Fact]
    public async Task Unknown_incident_is_404()
    {
        using var response = await _dashboard.Client().GetAsync(new Uri("/api/incidents/404", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Incidents_are_503_without_a_database()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        using var response = await factory.CreateClient().GetAsync(new Uri("/api/incidents", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
