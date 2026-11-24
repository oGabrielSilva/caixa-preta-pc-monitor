using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;

/// <summary>
/// Builds one incident's diagnosis input: the incident, the telemetry window by sensor role across every segment of its
/// session, and the related events it owns. The diagnosis and the dashboard read incidents through it, so both see the
/// same events and the same window.
/// </summary>
internal static class IncidentEvidenceAssembler
{
    /// <summary>The input of one incident, or <c>null</c> when it does not exist.</summary>
    public static IncidentEvidence? Load(SqliteConnection connection, long incidentId)
    {
        var row = connection.QuerySingleOrDefault<IncidentRow>(DiagnosisSql.Incident, new { IncidentId = incidentId });
        return row is null ? null : Assemble(connection, row);
    }

    private static IncidentEvidence Assemble(SqliteConnection connection, IncidentRow row)
    {
        // Older incidents were stored with wide event windows; keep only the events this incident owns.
        var loggedAt = Time(row.LoggedAtMs);
        var previous = TimeOrNull(connection.ExecuteScalar<long?>(DiagnosisSql.PreviousLoggedAt, new { row.LoggedAtMs }));
        IReadOnlyList<SystemEvent> related = [.. connection
            .Query<EventRow>(DiagnosisSql.RelatedEvents, new { IncidentId = row.Id })
            .Select(e => new SystemEvent(e.Provider, (int)e.EventId, e.RecordId, Time(e.LoggedAtMs), (int)e.Level, DiagnosisJson.Data(e.DataJson)))
            .Where(e => RelatedEventScope.Belongs(e, loggedAt, previous))];

        var shutdown = new UnexpectedShutdown(
            row.EventRecordId, loggedAt, row.BugcheckCode, (int)row.SleepState, TimeOrNull(row.PowerButtonMs),
            row.LongPowerButton != 0, TimeOrNull(row.LastAliveMs), DiagnosisJson.Data(row.DataJson));
        var incident = new Incident(shutdown, TimeOrNull(row.EstimatedFailureMs), Enum.Parse<FailureTimeSource>(row.FailureTimeSource),
            row.SessionId, row.SegmentId, TimeOrNull(row.TelemetryEndMs), related);

        return new IncidentEvidence(row.Id, incident, Telemetry(connection, row));
    }

    private static TelemetryWindow? Telemetry(SqliteConnection connection, IncidentRow row)
    {
        if (row.SessionId is not { } sessionId || row.TelemetryEndMs is not { } endMs)
        {
            return null;
        }

        var frames = connection.Query<FrameRow>(DiagnosisSql.WindowFrames, new
        {
            SessionId = sessionId,
            StartMs = endMs - (long)DiagnosisThresholds.Window.TotalMilliseconds,
            EndMs = endMs,
        }).ToList();

        var rolesBySegment = frames.Select(f => f.SegmentId).Distinct().ToDictionary(
            segment => segment,
            segment => connection.Query<(long Ordinal, string Role)>(DiagnosisSql.SegmentRoles, new { SegmentId = segment })
                .Select(r => (r.Ordinal, Role: Enum.Parse<SensorRole>(r.Role)))
                .Where(r => r.Role != SensorRole.None)
                .ToList());

        var series = new Dictionary<SensorRole, List<(DateTimeOffset At, float Value)>>();
        foreach (var frame in frames)
        {
            var values = FrameCodec.Decode(frame.Values);
            foreach (var (ordinal, role) in rolesBySegment[frame.SegmentId].Where(r => r.Ordinal < values.Length))
            {
                if (!series.TryGetValue(role, out var points))
                {
                    series[role] = points = [];
                }

                points.Add((Time(frame.TimestampMs), values[ordinal]));
            }
        }

        return new TelemetryWindow(Time(endMs), series.ToDictionary(s => s.Key, s => (IReadOnlyList<(DateTimeOffset, float)>)s.Value));
    }

    private static DateTimeOffset Time(long milliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

    private static DateTimeOffset? TimeOrNull(long? milliseconds) => milliseconds is { } ms ? Time(ms) : null;

    // Settable-property rows: Dapper cannot match positional constructors when SQLite reports computed or NULL
    // columns with a different type (see SqliteRetentionStore).
    private sealed class IncidentRow
    {
        public long Id { get; init; }

        public long EventRecordId { get; init; }

        public long LoggedAtMs { get; init; }

        public long? EstimatedFailureMs { get; init; }

        public string FailureTimeSource { get; init; } = string.Empty;

        public long? SessionId { get; init; }

        public long? SegmentId { get; init; }

        public long? TelemetryEndMs { get; init; }

        public long BugcheckCode { get; init; }

        public long SleepState { get; init; }

        public long? PowerButtonMs { get; init; }

        public long LongPowerButton { get; init; }

        public long? LastAliveMs { get; init; }

        public string DataJson { get; init; } = "{}";
    }

    private sealed class EventRow
    {
        public string Provider { get; init; } = string.Empty;

        public long EventId { get; init; }

        public long RecordId { get; init; }

        public long LoggedAtMs { get; init; }

        public long Level { get; init; }

        public string DataJson { get; init; } = "{}";
    }

    private sealed class FrameRow
    {
        public long SegmentId { get; init; }

        public long TimestampMs { get; init; }

        public byte[] Values { get; init; } = [];
    }
}
