using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchModel = ArchUnitNET.Domain.Architecture;
using ReflectionAssembly = System.Reflection.Assembly;

namespace PcMonitor.Architecture.Tests;

/// <summary>
/// Loads the production assemblies once and exposes them as ArchUnitNET type sets, one per layer.
/// </summary>
internal static class PcMonitorArchitecture
{
    public const string DomainAssembly = "PcMonitor.Domain";
    public const string ApplicationAssembly = "PcMonitor.Application";
    public const string SensorsAdapterAssembly = "PcMonitor.Infrastructure.Sensors.Lhm";
    public const string PersistenceAdapterAssembly = "PcMonitor.Infrastructure.Persistence.Sqlite";
    public const string WindowsAdapterAssembly = "PcMonitor.Infrastructure.Windows";
    public const string CollectorAssembly = "PcMonitor.Collector";
    public const string DashboardAssembly = "PcMonitor.Dashboard";

    public static readonly IReadOnlyList<string> AdapterAssemblies =
        [SensorsAdapterAssembly, PersistenceAdapterAssembly, WindowsAdapterAssembly];

    private static readonly string[] _productionAssemblies =
    [
        DomainAssembly, ApplicationAssembly, SensorsAdapterAssembly, PersistenceAdapterAssembly,
        WindowsAdapterAssembly, CollectorAssembly, DashboardAssembly,
    ];

    public static readonly ArchModel Model = new ArchLoader()
        .LoadAssemblies([.. _productionAssemblies.Select(ReflectionAssembly.Load)])
        .Build();

    /// <summary>Types declared in any of the given production assemblies.</summary>
    public static GivenTypesConjunction TypesIn(string first, params string[] more) =>
        Types().That().ResideInAssembly(Load(first), [.. more.Select(Load)]);

    /// <summary>Types declared outside the given production assembly.</summary>
    public static GivenTypesConjunction TypesOutside(string assemblyName) =>
        Types().That().DoNotResideInAssembly(Load(assemblyName));

    /// <summary>
    /// Types from the LibreHardwareMonitor library. They are only referenced, never loaded,
    /// so the set must include referenced types (<c>Types(true)</c>) or the rule is vacuous.
    /// </summary>
    public static GivenTypesConjunction LibreHardwareMonitorTypes() =>
        Types(true).That().ResideInNamespaceMatching(@"^LibreHardwareMonitor(\..*)?$");

    public static ReflectionAssembly Load(string assemblyName) => ReflectionAssembly.Load(assemblyName);
}
