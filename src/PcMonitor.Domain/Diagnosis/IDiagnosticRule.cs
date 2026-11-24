namespace PcMonitor.Domain.Diagnosis;

/// <summary>One independent check. Adding a rule never requires changing the engine.</summary>
public interface IDiagnosticRule
{
    IEnumerable<Finding> Evaluate(IncidentEvidence evidence);
}
