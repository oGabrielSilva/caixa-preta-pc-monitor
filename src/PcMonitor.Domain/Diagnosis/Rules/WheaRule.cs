using System.Globalization;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Hardware errors reported by the processor or memory (WHEA). Their absence is not evidence: a power cut erases the
/// hardware error records before Windows writes them.
/// </summary>
public sealed class WheaRule : IDiagnosticRule
{
    private const string WheaProvider = "Microsoft-Windows-WHEA-Logger";

    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var whea = evidence.Incident.RelatedEvents.Where(e => e.Provider == WheaProvider).ToList();
        if (whea.Count > 0)
        {
            yield return Finding.For("whea.event", Hypothesis.CpuOrMemory, EvidenceStrength.Strong, FindingParameters.Of(
                ("count", whea.Count), ("ids", string.Join(',', whea.Select(e => e.EventId).Distinct()))));
        }

        if (evidence.Incident.Shutdown.Data.TryGetValue("WHEABootErrorCount", out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0)
        {
            yield return Finding.For("whea.boot_errors", Hypothesis.CpuOrMemory, EvidenceStrength.Moderate,
                FindingParameters.Of(("count", count)));
        }
    }
}
