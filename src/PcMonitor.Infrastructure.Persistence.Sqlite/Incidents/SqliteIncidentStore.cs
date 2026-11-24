using System.Text.Json;
using Dapper;
using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary><see cref="IIncidentStore"/> on SQLite. Used once per collector start, so it opens a connection per call.</summary>
public sealed class SqliteIncidentStore(SqliteDatabase database) : IIncidentStore
{
    public DateTimeOffset? LatestIncidentLoggedAt()
    {
        using var connection = database.OpenWriter();
        return connection.ExecuteScalar<long?>(IncidentSql.LatestLoggedAt) is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;
    }

    public IReadOnlyList<OpenSession> OpenSessions()
    {
        using var connection = database.OpenWriter();
        return [.. connection.Query<OpenSessionRow>(IncidentSql.OpenSessions).Select(row => new OpenSession(
            row.SessionId,
            DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAtMs),
            row.LastSegmentId,
            row.OpenSegmentId,
            row.LastFrameMs is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null))];
    }

    public bool SaveIncident(Incident incident)
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
            shutdown.BugcheckCode,
            shutdown.SleepInProgress,
            PowerButtonMs = shutdown.PowerButtonPressedAt?.ToUnixTimeMilliseconds(),
            LongPowerButton = shutdown.LongPowerButtonPress,
            LastAliveMs = shutdown.LastAliveAt?.ToUnixTimeMilliseconds(),
            DataJson = JsonSerializer.Serialize(shutdown.Data),
        }, transaction);

        if (incidentId is null)
        {
            return false;
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

        transaction.Commit();
        return true;
    }

    public void EndSession(OpenSession session, DateTimeOffset at, EndReason reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        using var connection = database.OpenWriter();
        using var transaction = connection.BeginTransaction();
        var parameters = new { session.SessionId, SegmentId = session.OpenSegmentId, AtMs = at.ToUnixTimeMilliseconds(), Reason = reason.ToString() };

        if (session.OpenSegmentId is not null)
        {
            connection.Execute(IncidentSql.EndSegment, parameters, transaction);
        }

        connection.Execute(IncidentSql.EndSession, parameters, transaction);
        transaction.Commit();
    }

    // Settable properties instead of a positional record: Dapper cannot match constructor types for computed
    // columns when the query returns no rows (see SqliteRetentionStore).
    private sealed class OpenSessionRow
    {
        public long SessionId { get; init; }

        public long StartedAtMs { get; init; }

        public long? LastSegmentId { get; init; }

        public long? OpenSegmentId { get; init; }

        public long? LastFrameMs { get; init; }
    }
}
