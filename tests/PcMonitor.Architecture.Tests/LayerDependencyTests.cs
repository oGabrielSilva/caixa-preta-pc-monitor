using ArchUnitNET.xUnitV3;
using static PcMonitor.Architecture.Tests.PcMonitorArchitecture;

namespace PcMonitor.Architecture.Tests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_references_only_the_base_class_library()
    {
        var referenced = Load(DomainAssembly).GetReferencedAssemblies().Select(a => a.Name!);

        referenced.ShouldAllBe(name => name.StartsWith("System") || name == "netstandard");
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_hosts() =>
        TypesIn(ApplicationAssembly)
            .Should().NotDependOnAny(TypesIn(
                SensorsAdapterAssembly, PersistenceAdapterAssembly, WindowsAdapterAssembly,
                CollectorAssembly, DashboardAssembly))
            .WithoutRequiringPositiveResults()
            .Check(Model);

    [Theory]
    [InlineData(SensorsAdapterAssembly)]
    [InlineData(PersistenceAdapterAssembly)]
    [InlineData(WindowsAdapterAssembly)]
    public void Infrastructure_adapters_do_not_depend_on_each_other(string adapter)
    {
        var others = AdapterAssemblies.Where(a => a != adapter).ToArray();

        TypesIn(adapter)
            .Should().NotDependOnAny(TypesIn(others[0], others[1..]))
            .WithoutRequiringPositiveResults()
            .Check(Model);
    }

    [Fact]
    public void Dashboard_does_not_depend_on_sensor_or_windows_adapters() =>
        TypesIn(DashboardAssembly)
            .Should().NotDependOnAny(TypesIn(SensorsAdapterAssembly, WindowsAdapterAssembly))
            .WithoutRequiringPositiveResults()
            .Check(Model);

    [Fact]
    public void Only_the_sensor_adapter_uses_LibreHardwareMonitor() =>
        TypesOutside(SensorsAdapterAssembly)
            .Should().NotDependOnAny(LibreHardwareMonitorTypes())
            .WithoutRequiringPositiveResults()
            .Check(Model);
}
