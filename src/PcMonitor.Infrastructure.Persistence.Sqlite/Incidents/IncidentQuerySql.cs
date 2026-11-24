namespace PcMonitor.Infrastructure.Persistence.Sqlite.Incidents;

/// <summary>SQL for listing incidents and reading their stored diagnosis.</summary>
internal static class IncidentQuerySql
{
    private const string SummarySelect = """
        SELECT i.id AS Id, i.logged_at_ms AS LoggedAtMs, i.estimated_failure_ms AS EstimatedFailureMs,
               i.failure_time_source AS FailureTimeSource, i.sleep_in_progress AS SleepState, i.bugcheck_code AS BugcheckCode,
               i.telemetry_end_ms AS TelemetryEndMs, d.top_hypothesis AS TopHypothesis, d.confidence AS Confidence,
               d.findings_json AS FindingsJson
        FROM incident i LEFT JOIN diagnosis d ON d.incident_id = i.id
        """;

    public const string Summaries = SummarySelect + " ORDER BY i.logged_at_ms DESC, i.id DESC;";

    public const string Summary = SummarySelect + " WHERE i.id = @Id;";

    public const string Diagnosis = """
        SELECT engine_version AS EngineVersion, diagnosed_at_ms AS DiagnosedAtMs, ranking_json AS RankingJson,
               findings_json AS FindingsJson, limitations_json AS LimitationsJson
        FROM diagnosis WHERE incident_id = @Id;
        """;
}
