using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PcMonitor.Dashboard.Tests.Hosting;

/// <summary>Runs the dashboard on real Kestrel to check which addresses it actually binds.</summary>
public sealed class LoopbackBindingTests
{
    [Fact]
    public void Dashboard_binds_only_to_loopback_whatever_the_configuration_says()
    {
        var port = FreeTcpPort();
        var lanPort = FreeTcpPort();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Dashboard:Port", port.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .UseSetting("urls", $"http://0.0.0.0:{lanPort}")
            .UseSetting("Kestrel:Endpoints:Lan:Url", $"http://0.0.0.0:{lanPort}"));
        factory.UseKestrel();
        factory.StartServer();

        var addresses = factory.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;

        addresses.ShouldBe([$"http://127.0.0.1:{port}"]);
    }

    private static int FreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
