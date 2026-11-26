using PcMonitor.Tray.Startup;

namespace PcMonitor.Tray.Tests;

public sealed class DashboardAddressTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("caixa-preta-dashboard-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void The_port_comes_from_the_installed_dashboard_settings()
    {
        File.WriteAllText(Path.Combine(_folder, "appsettings.json"), """{ "Dashboard": { "Port": 5300 } }""");

        DashboardAddress.Read(_folder).ShouldBe(new Uri("http://127.0.0.1:5300/"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ broken")]
    [InlineData("""{ "Logging": {} }""")]
    public void Without_a_readable_port_the_default_5280_is_used(string? content)
    {
        if (content is not null)
        {
            File.WriteAllText(Path.Combine(_folder, "appsettings.json"), content);
        }

        DashboardAddress.Read(_folder).ShouldBe(new Uri("http://127.0.0.1:5280/"));
    }

    [Fact]
    public void A_dashboard_argument_overrides_the_installed_address() =>
        DashboardAddress.FromArguments(["--background", "--dashboard", "http://localhost:3100"], _folder).ShouldBe(new Uri("http://localhost:3100/"));

    [Fact]
    public void Without_the_argument_the_installed_address_is_used() =>
        DashboardAddress.FromArguments(["--background"], _folder).ShouldBe(new Uri("http://127.0.0.1:5280/"));
}
