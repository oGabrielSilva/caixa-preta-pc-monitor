using Dapper;
using PcMonitor.Application.Diagnosis;
using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;

/// <summary>Diagnosis storage on SQLite. Each incident's input is built by <see cref="IncidentEvidenceAssembler"/>.</summary>
public sealed class SqliteDiagnosisStore(SqliteDatabase database, TimeProvider time) : IDiagnosisInputReader, IDiagnosisWriter
{
    public IReadOnlyList<long> IncidentsNeedingDiagnosis(int engineVersion)
    {
        using var connection = database.OpenWriter();
        return [.. connection.Query<long>(DiagnosisSql.IncidentsNeedingDiagnosis, new { EngineVersion = engineVersion })];
    }

    public IncidentEvidence Load(long incidentId)
    {
        using var connection = database.OpenWriter();
        return IncidentEvidenceAssembler.Load(connection, incidentId)
            ?? throw new InvalidOperationException($"Incident {incidentId} does not exist.");
    }

    public void Save(long incidentId, IncidentDiagnosis diagnosis)
    {
        ArgumentNullException.ThrowIfNull(diagnosis);
        using var connection = database.OpenWriter();
        connection.Execute(DiagnosisSql.Upsert, new
        {
            IncidentId = incidentId,
            diagnosis.EngineVersion,
            DiagnosedAtMs = time.GetUtcNow().ToUnixTimeMilliseconds(),
            TopHypothesis = diagnosis.Top?.ToString(),
            Confidence = diagnosis.Confidence.ToString(),
            RankingJson = DiagnosisJson.Ranking(diagnosis.Ranking),
            FindingsJson = DiagnosisJson.Findings(diagnosis.Findings),
            LimitationsJson = DiagnosisJson.Findings(diagnosis.Limitations),
        });
    }
}
