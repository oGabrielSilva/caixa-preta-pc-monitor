using System.Reflection;

namespace PcMonitor.Dashboard.Api;

/// <summary>Semantic version of the running dashboard, without build metadata.</summary>
internal static class ApplicationVersion
{
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var informational = typeof(ApplicationVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return informational?.Split('+')[0] ?? "0.0.0";
    }
}
