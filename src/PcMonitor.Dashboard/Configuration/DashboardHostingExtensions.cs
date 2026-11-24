using System.Net;
using Microsoft.Extensions.Options;

namespace PcMonitor.Dashboard.Configuration;

internal static class DashboardHostingExtensions
{
    /// <summary>
    /// Registers validated <see cref="DashboardOptions"/> and binds Kestrel to <c>127.0.0.1</c> only.
    /// </summary>
    /// <remarks>
    /// The explicit <c>Listen</c> overrides <c>ASPNETCORE_URLS</c> and <c>--urls</c>. Endpoints declared in the
    /// <c>Kestrel</c> configuration section would still be added on top of it, so the configuration loader is
    /// replaced with an empty one. Configuration therefore cannot expose the dashboard to the network.
    /// </remarks>
    public static WebApplicationBuilder AddLoopbackDashboard(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<DashboardOptions>()
            .BindConfiguration(DashboardOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Configure(new ConfigurationBuilder().Build(), reloadOnChange: false);

            var options = kestrel.ApplicationServices.GetRequiredService<IOptions<DashboardOptions>>().Value;
            kestrel.Listen(IPAddress.Loopback, options.Port);
        });

        return builder;
    }
}
