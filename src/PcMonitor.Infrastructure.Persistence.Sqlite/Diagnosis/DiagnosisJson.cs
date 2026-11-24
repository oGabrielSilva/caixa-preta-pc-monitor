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

    public static IReadOnlyList<HypothesisScore> ReadRanking(string json) =>
        [.. (JsonSerializer.Deserialize<List<StoredScore>>(json, _options) ?? []).Select(s => new HypothesisScore(s.Hypothesis, s.Score))];

    public static IReadOnlyList<Finding> ReadFindings(string json) =>
        [.. (JsonSerializer.Deserialize<List<StoredFinding>>(json, _options) ?? []).Select(f => f.ToFinding())];

    public static Dictionary<string, string> Data(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

    private sealed record StoredScore(Hypothesis Hypothesis, int Score);

    /// <summary>Observations and limitations are stored without <c>supports</c> and <c>strength</c>; their defaults match the <see cref="Finding"/> factories.</summary>
    private sealed record StoredFinding(
        string Code, FindingKind Kind, Hypothesis? Hypothesis, bool? Supports, EvidenceStrength? Strength, Dictionary<string, string>? Parameters)
    {
        public Finding ToFinding() =>
            new(Code, Kind, Hypothesis, Supports ?? true, Strength ?? EvidenceStrength.Weak, Parameters ?? []);
    }
}
