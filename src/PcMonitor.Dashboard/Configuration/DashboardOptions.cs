using System.ComponentModel.DataAnnotations;

namespace PcMonitor.Dashboard.Configuration;

/// <summary>
/// Dashboard settings bound from the <c>Dashboard</c> configuration section.
/// The listen address is always the IPv4 loopback; only the port is configurable.
/// </summary>
public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    [Range(1024, 65535)]
    public int Port { get; init; } = 5280;
}
