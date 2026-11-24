using System.Globalization;
using static PcMonitor.Domain.Diagnosis.DiagnosisThresholds;

namespace PcMonitor.Domain.Diagnosis.Rules;

/// <summary>
/// Hardware errors reported through WHEA. One machine check usually shows up several ways (a 0x124 blue screen, a
/// WHEA-Logger event, a boot error count), so they form <em>one</em> fact weighted by its strongest source. Power
/// and VRM instability also surface as WHEA errors, hence the caveat. The absence of WHEA is not evidence: a power
/// cut erases the hardware error records before Windows writes them.
/// </summary>
public sealed class WheaRule : IDiagnosticRule
{
    private const string WheaProvider = "Microsoft-Windows-WHEA-Logger";

    /// <summary>Fatal or uncorrected hardware errors.</summary>
    private static readonly HashSet<int> _fatalEvents = [1, 18, 20, 46];

    /// <summary>Corrected machine-check and memory errors.</summary>
    private static readonly HashSet<int> _correctedEvents = [19, 47];

    /// <summary>Corrected PCIe errors: a device or slot, not the CPU or memory.</summary>
    private const int PcieEvent = 17;

    public IEnumerable<Finding> Evaluate(IncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var shutdown = evidence.Incident.Shutdown;
        var events = evidence.Incident.RelatedEvents.Where(e => e.Provider == WheaProvider).ToList();
        var sources = new List<(string Source, EvidenceStrength Strength)>();

        if (shutdown.BugcheckCode == WheaBugcheck)
        {
            sources.Add(($"bugcheck:0x{shutdown.BugcheckCode:X8}", EvidenceStrength.Strong));
        }

        foreach (var id in events.Select(e => e.EventId).Distinct().Order())
        {
            if (_fatalEvents.Contains(id))
            {
                sources.Add(($"event:{id}", EvidenceStrength.Strong));
            }
            else if (_correctedEvents.Contains(id))
            {
                sources.Add(($"event:{id}", EvidenceStrength.Weak));
            }
        }

        if (shutdown.Data.TryGetValue("WHEABootErrorCount", out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0)
        {
            sources.Add(($"boot_errors:{count}", EvidenceStrength.Moderate));
        }

        if (sources.Count > 0)
        {
            yield return Finding.For("whea.hardware_error", Hypothesis.CpuOrMemory, sources.Max(s => s.Strength),
                FindingParameters.Of(("sources", string.Join(',', sources.Select(s => s.Source)))));
            yield return Finding.Limitation("whea.may_be_power");
        }

        if (events.Any(e => e.EventId == PcieEvent))
        {
            yield return Finding.Observation("whea.pcie");
        }
    }
}
