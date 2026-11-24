using Dapper;
using PcMonitor.Application.Monitoring;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;
using PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

namespace PcMonitor.Tests.Support;

/// <summary>
/// Writes realistic data through the real stores: a recording of this machine's sensors, incidents with their Windows
/// events and diagnoses made by the real engine. The 1074 event carries a computer and a user name, so tests can prove
/// those never reach the dashboard.
/// </summary>
public sealed class DatabaseSeed(SqliteDatabase database) : IDisposable
{
    public const string ComputerName = "DESKTOP-TEST";
    public const string UserName = "tester";

    /// <summary>The sensors of this board, in frame order: CPU temperature, load, power, +12 V, +5 V, +3.3 V, one fan.</summary>
    public static readonly SensorCatalog Catalog = new(
    [
        new SensorDescriptor("/amdcpu/0/temperature/2", "CPU (Tctl/Tdie)", SensorKind.Temperature, "°C", SensorRole.CpuTemperature),
        new SensorDescriptor("/amdcpu/0/load/0", "CPU Total", SensorKind.Load, "%", SensorRole.CpuLoad),
        new SensorDescriptor("/amdcpu/0/power/0", "CPU Package", SensorKind.Power, "W", SensorRole.CpuPackagePower),
        new SensorDescriptor("/lpc/nct6799d/0/voltage/4", "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V),
        new SensorDescriptor("/lpc/nct6799d/0/voltage/1", "+5V", SensorKind.Voltage, "V", SensorRole.Rail5V),
        new SensorDescriptor("/lpc/nct6799d/0/voltage/3", "+3.3V", SensorKind.Voltage, "V", SensorRole.Rail3V3),
        new SensorDescriptor("/lpc/nct6799d/0/fan/1", "Fan #2", SensorKind.Fan, "RPM", SensorRole.None),
    ]);

    private readonly SqliteMonitoringStore _monitoring = new(database);

    /// <summary>A typical idle reading at second <paramref name="second"/>.</summary>
    public static float[] Idle(int second) => [45f + (second % 3), 6f, 20f, 12.19f, 5f, 3.296f, 2000f];

    public long StartSession(DateTimeOffset startedAt, SensorSourceStatus status = SensorSourceStatus.Ok) =>
        _monitoring.StartSession(startedAt, startedAt.AddSeconds(-30), new SensorSourceInfo(status, "TUF GAMING A620M-PLUS WIFI", Catalog));

    public long StartSegment(long sessionId, DateTimeOffset startedAt, SensorCatalog? catalog = null) =>
        _monitoring.StartSegment(sessionId, startedAt, catalog ?? Catalog);

    /// <summary>One frame per second from <paramref name="start"/>, values from <paramref name="values"/> (default <see cref="Idle"/>).</summary>
    public void Frames(long segmentId, DateTimeOffset start, int count, Func<int, float[]>? values = null)
    {
        for (var second = 0; second < count; second++)
        {
            _monitoring.AppendFrame(segmentId, new Frame(start.AddSeconds(second), (values ?? Idle)(second)));
        }
    }

    /// <summary>A session with one segment recording <paramref name="seconds"/> idle frames.</summary>
    public (long SessionId, long SegmentId) Recording(DateTimeOffset start, int seconds)
    {
        var session = StartSession(start);
        var segment = StartSegment(session, start);
        Frames(segment, start, seconds);
        return (session, segment);
    }

    public void EndSession(long sessionId, DateTimeOffset at, EndReason reason = EndReason.ServiceStopped) =>
        _monitoring.EndSession(sessionId, at, reason);

    /// <summary>The 1074 event ("restart initiated by"), whose data names the computer and the user.</summary>
    public static SystemEvent PrivateShutdownEvent(long record, DateTimeOffset at) =>
        new("User32", 1074, record, at, 4, new Dictionary<string, string>
        {
            ["param1"] = @$"C:\Windows\System32\RuntimeBroker.exe ({ComputerName})",
            ["param7"] = @$"{ComputerName}\{UserName}",
        });

    /// <summary>Saves an incident and returns its id.</summary>
    public long Incident(
        long record,
        DateTimeOffset loggedAt,
        FailureTimeSource source = FailureTimeSource.Unknown,
        DateTimeOffset? estimate = null,
        long? sessionId = null,
        long? segmentId = null,
        DateTimeOffset? telemetryEnd = null,
        int sleepState = 0,
        params SystemEvent[] events)
    {
        var shutdown = new UnexpectedShutdown(record, loggedAt, 0, sleepState, null, false, estimate,
            new Dictionary<string, string> { ["SleepInProgress"] = sleepState.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        new SqliteIncidentStore(database).SaveIncident(new Incident(shutdown, estimate, source, sessionId, segmentId, telemetryEnd, events), null);

        using var connection = database.OpenReader();
        return connection.ExecuteScalar<long>(
            "SELECT id FROM incident WHERE event_record_id = @Record AND logged_at_ms = @LoggedAtMs",
            new { Record = record, LoggedAtMs = loggedAt.ToUnixTimeMilliseconds() });
    }

    /// <summary>Diagnoses the incident with the real engine and rules, as the collector does.</summary>
    public IncidentDiagnosis Diagnose(long incidentId, TimeProvider time)
    {
        var store = new SqliteDiagnosisStore(database, time);
        var diagnosis = new DiagnosisEngine(DiagnosticRules.All).Diagnose(store.Load(incidentId));
        store.Save(incidentId, diagnosis);
        return diagnosis;
    }

    public void Dispose() => _monitoring.Dispose();
}
