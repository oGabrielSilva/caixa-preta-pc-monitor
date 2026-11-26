using System.IO;
using System.Text.Json;

namespace PcMonitor.Tray.Startup;

/// <summary>The local address of the dashboard, from the installed dashboard's settings.</summary>
internal static class DashboardAddress
{
    private const int DefaultPort = 5280;

    public static string DefaultFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PcMonitor", "Dashboard");

    /// <summary><c>http://127.0.0.1:{port}/</c>, the port read from <c>appsettings.json</c> in <paramref name="dashboardFolder"/> (5280 when absent).</summary>
    public static Uri Read(string dashboardFolder) => new($"http://127.0.0.1:{PortIn(Path.Combine(dashboardFolder, "appsettings.json"))}/");

    /// <summary>The address given by <c>--dashboard &lt;url&gt;</c> (development), else the installed dashboard's.</summary>
    public static Uri FromArguments(IReadOnlyList<string> arguments, string dashboardFolder)
    {
        var index = arguments.ToList().IndexOf("--dashboard");
        return index >= 0 && index + 1 < arguments.Count && Uri.TryCreate(arguments[index + 1].TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            ? uri
            : Read(dashboardFolder);
    }

    private static int PortIn(string settings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settings));
            return document.RootElement.TryGetProperty("Dashboard", out var dashboard)
                && dashboard.TryGetProperty("Port", out var port) && port.TryGetInt32(out var value)
                ? value
                : DefaultPort;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return DefaultPort;
        }
    }
}
