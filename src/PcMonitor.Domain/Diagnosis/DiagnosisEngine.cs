using PcMonitor.Domain.Incidents;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Runs every rule, adds up the evidence per hypothesis and rates how much the top one stands out.</summary>
public sealed class DiagnosisEngine(IEnumerable<IDiagnosticRule> rules)
{
    /// <summary>Bump whenever a rule or threshold changes: stored diagnoses of older versions are redone.</summary>
    public const int Version = 1;

    private const int HighScore = 5;
    private const int HighMargin = 3;
    private const int MediumScore = 3;

    private readonly IReadOnlyList<IDiagnosticRule> _rules = [.. rules];

    public IncidentDiagnosis Diagnose(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var all = _rules.SelectMany(rule => rule.Evaluate(evidence)).Concat(InputLimitations(evidence)).ToList();

        var ranking = all
            .Where(f => f.Kind == FindingKind.Evidence)
            .GroupBy(f => f.Hypothesis!.Value)
            .Select(g => new HypothesisScore(g.Key, g.Sum(f => f.Weight)))
            .Where(s => s.Score != 0)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Hypothesis)
            .ToList();

        var limitations = new List<Finding>();
        foreach (var limitation in all.Where(f => f.Kind == FindingKind.Limitation))
        {
            if (!limitations.Any(l => l.SameAs(limitation)))
            {
                limitations.Add(limitation);
            }
        }

        return new IncidentDiagnosis(Version, ranking, Rate(ranking), [.. all.Where(f => f.Kind != FindingKind.Limitation)], limitations);
    }

    private static IEnumerable<Finding> InputLimitations(IncidentEvidence evidence)
    {
        if (evidence.Telemetry is null)
        {
            yield return Finding.Limitation("telemetry.missing");
        }

        if (evidence.Incident.FailureTimeSource is FailureTimeSource.LastAliveEventLog or FailureTimeSource.LastFrameBeforeSleep)
        {
            yield return Finding.Limitation("estimate.lower_bound");
        }
    }

    private static Confidence Rate(List<HypothesisScore> ranking)
    {
        var top = ranking.Count > 0 ? ranking[0].Score : 0;
        var second = ranking.Count > 1 ? Math.Max(ranking[1].Score, 0) : 0;
        return top switch
        {
            <= 0 => Confidence.Inconclusive,
            >= HighScore when top - second >= HighMargin => Confidence.High,
            >= MediumScore => Confidence.Medium,
            _ => Confidence.Low,
        };
    }
}
