using Dapper;
using PcMonitor.Application.Incidents;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary>
/// <see cref="IIncidentQuery"/> on SQLite, through read-only connections. Events and telemetry come from
/// <see cref="IncidentEvidenceAssembler"/>, the same input the diagnosis evaluated; event data is dropped here.
/// </summary>
public sealed class SqliteIncidentQuery(SqliteDatabase database) : IIncidentQuery
{
    public IReadOnlyList<IncidentSummary> List()
    {
        using var connection = database.OpenReader();
        return [.. connection.Query<SummaryRow>(IncidentQuerySql.Summaries).Select(row => row.ToSummary())];
    }

    public IncidentDetail? Find(long id)
    {
        using var connection = database.OpenReader();
        var summary = connection.QuerySingleOrDefault<SummaryRow>(IncidentQuerySql.Summary, new { Id = id });
        var evidence = summary is null ? null : IncidentEvidenceAssembler.Load(connection, id);
        if (summary is null || evidence is null)
        {
            return null;
        }

        var diagnosis = connection.QuerySingleOrDefault<DiagnosisRow>(IncidentQuerySql.Diagnosis, new { Id = id });
        return new IncidentDetail(
            summary.ToSummary(),
            evidence.Incident.Shutdown.LastAliveAt,
            evidence.Incident.TelemetryEndAt,
            [.. evidence.Incident.RelatedEvents.Select(e => new RelatedEventView(e.Provider, e.EventId, e.LoggedAt, e.Level))],
            diagnosis?.ToDiagnosis(),
            evidence.Telemetry,
            evidence.AtFailure is not null);
    }

    private static DateTimeOffset Time(long milliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

    // Settable-property rows: Dapper cannot match positional constructors when SQLite reports NULL columns.
    private sealed class SummaryRow
    {
        public long Id { get; init; }

        public long LoggedAtMs { get; init; }

        public long? EstimatedFailureMs { get; init; }

        public string FailureTimeSource { get; init; } = string.Empty;

        public long SleepState { get; init; }

        public long BugcheckCode { get; init; }

        public long? TelemetryEndMs { get; init; }

        public string? TopHypothesis { get; init; }

        public string? Confidence { get; init; }

        public string? FindingsJson { get; init; }

        public IncidentSummary ToSummary() => new(
            Id,
            Time(LoggedAtMs),
            EstimatedFailureMs is { } estimate ? Time(estimate) : null,
            Enum.Parse<FailureTimeSource>(FailureTimeSource),
            (int)SleepState,
            BugcheckCode,
            TelemetryEndMs is not null,
            TopHypothesis is null ? null : Enum.Parse<Hypothesis>(TopHypothesis),
            Confidence is null ? null : Enum.Parse<Confidence>(Confidence),
            FindingsJson is null
                ? []
                : [.. DiagnosisJson.ReadFindings(FindingsJson).Where(f => f.Kind == FindingKind.Observation).Select(f => f.Code).Distinct()]);
    }

    private sealed class DiagnosisRow
    {
        public long EngineVersion { get; init; }

        public long DiagnosedAtMs { get; init; }

        public string RankingJson { get; init; } = "[]";

        public string FindingsJson { get; init; } = "[]";

        public string LimitationsJson { get; init; } = "[]";

        public StoredDiagnosis ToDiagnosis() => new(
            (int)EngineVersion,
            Time(DiagnosedAtMs),
            DiagnosisJson.ReadRanking(RankingJson),
            DiagnosisJson.ReadFindings(FindingsJson),
            DiagnosisJson.ReadFindings(LimitationsJson));
    }
}
