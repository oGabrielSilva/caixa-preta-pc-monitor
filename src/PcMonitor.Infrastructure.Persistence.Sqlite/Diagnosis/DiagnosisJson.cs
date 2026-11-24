using System.Text.Json;
using System.Text.Json.Serialization;
using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Diagnosis;

/// <summary>The stored JSON shape of rankings and findings: codes, enums as names, parameters as strings.</summary>
internal static class DiagnosisJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Ranking(IReadOnlyList<HypothesisScore> ranking) =>
        JsonSerializer.Serialize(ranking.Select(s => new { s.Hypothesis, s.Score }), _options);

    public static string Findings(IReadOnlyList<Finding> findings) =>
        JsonSerializer.Serialize(findings.Select(f => new
        {
            f.Code,
            f.Kind,
            f.Hypothesis,
            Supports = f.Kind == FindingKind.Evidence ? f.Supports : (bool?)null,
            Strength = f.Kind == FindingKind.Evidence ? f.Strength : (EvidenceStrength?)null,
            f.Parameters,
        }), _options);

    public static Dictionary<string, string> Data(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
}
