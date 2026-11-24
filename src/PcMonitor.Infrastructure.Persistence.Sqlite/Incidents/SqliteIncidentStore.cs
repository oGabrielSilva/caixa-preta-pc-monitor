using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary>
/// Incident storage on SQLite. Used once per collector start, so it opens a connection per call. Saving an incident
/// and ending the session that went down happen in one transaction: the database never says a session both crashed
/// and died abnormally.
/// </summary>
public sealed class SqliteIncidentStore(SqliteDatabase database) : IIncidentReader, IIncidentWriter
{
    public IReadOnlySet<IncidentKey> SavedIncidents()
    {
        using var connection = database.OpenWriter();
        return connection.Query<(long RecordId, long LoggedAtMs)>(IncidentSql.SavedKeys)
            .Select(row => new IncidentKey(row.RecordId, row.LoggedAtMs))
            .ToHashSet();
    }

    public IReadOnlyList<OpenSession> OpenSessions()
    {
        using var connection = database.OpenWriter();
        return [.. connection.Query<OpenSessionRow>(IncidentSql.OpenSessions).Select(row => new OpenSession(
            row.SessionId,
            DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAtMs),
            DateTimeOffset.FromUnixTimeMilliseconds(row.BootedAtMs),
            row.LastSegmentId,
            row.OpenSegmentId,
            FromMs(row.LastFrameMs)))];
    }

    public bool SaveIncident(Incident incident, SessionClosure? closure)
    {
        ArgumentNullException.ThrowIfNull(incident);
        var shutdown = incident.Shutdown;
        using var connection = database.OpenWriter();
        using var transaction = connection.BeginTransaction();

        var incidentId = connection.ExecuteScalar<long?>(IncidentSql.InsertIncident, new
        {
            shutdown.RecordId,
            LoggedAtMs = shutdown.LoggedAt.ToUnixTimeMilliseconds(),
            EstimatedFailureMs = incident.EstimatedFailureAt?.ToUnixTimeMilliseconds(),
            FailureTimeSource = incident.FailureTimeSource.ToString(),
            incident.SessionId,
            incident.SegmentId,
            TelemetryEndMs = incident.TelemetryEndAt?.ToUnixTimeMilliseconds(),
            shutdown.BugcheckCode,
            shutdown.SleepInProgress,
            PowerButtonMs = shutdown.PowerButtonPressedAt?.ToUnixTimeMilliseconds(),
            LongPowerButton = shutdown.LongPowerButtonPress,
            LastAliveMs = shutdown.LastAliveAt?.ToUnixTimeMilliseconds(),
            DataJson = JsonSerializer.Serialize(shutdown.Data),
        }, transaction);

        if (incidentId is null)
        {
            return false; // Already saved: its session was closed with it.
        }

        foreach (var related in incident.RelatedEvents)
        {
            connection.Execute(IncidentSql.InsertEvent, new
            {
                IncidentId = incidentId,
                related.Provider,
                related.EventId,
                related.RecordId,
                LoggedAtMs = related.LoggedAt.ToUnixTimeMilliseconds(),
                related.Level,
                DataJson = JsonSerializer.Serialize(related.Data),
            }, transaction);
        }

        if (closure is not null)
        {
            Close(connection, transaction, closure);
        }

        transaction.Commit();
        return true;
    }

    public void CloseSession(SessionClosure closure)
    {
        ArgumentNullException.ThrowIfNull(closure);
        using var connection = database.OpenWriter();
        using var transaction = connection.BeginTransaction();
        Close(connection, transaction, closure);
        transaction.Commit();
    }

    private static void Close(SqliteConnection connection, SqliteTransaction transaction, SessionClosure closure)
    {
        var endedAtMs = closure.At.ToUnixTimeMilliseconds();
        var reason = closure.Reason.ToString();
        if (closure.Session.OpenSegmentId is { } segmentId)
        {
            connection.Execute(MonitoringSql.EndSegment, new { Id = segmentId, EndedAtMs = endedAtMs, Reason = reason }, transaction);
        }

        connection.Execute(MonitoringSql.EndSession, new { Id = closure.Session.SessionId, EndedAtMs = endedAtMs, Reason = reason }, transaction);
    }

    private static DateTimeOffset? FromMs(long? milliseconds) =>
        milliseconds is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;

    // Settable properties instead of a positional record: Dapper cannot match constructor types for computed
    // columns when the query returns no rows (see SqliteRetentionStore).
    private sealed class OpenSessionRow
    {
        public long SessionId { get; init; }

        public long StartedAtMs { get; init; }

        public long BootedAtMs { get; init; }

        public long? LastSegmentId { get; init; }

        public long? OpenSegmentId { get; init; }

        public long? LastFrameMs { get; init; }
    }
}
