using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;

namespace PcMonitor.Domain.Tests.Diagnosis.Rules;

public sealed class DiagnosticRulesTests
{
    [Fact]
    public void Every_rule_is_registered_in_DiagnosticRules_All()
    {
        var ruleTypes = typeof(IDiagnosticRule).Assembly.GetTypes()
            .Where(t => typeof(IDiagnosticRule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        DiagnosticRules.All.Select(r => r.GetType()).ShouldBe(ruleTypes, ignoreOrder: true);
    }
}
