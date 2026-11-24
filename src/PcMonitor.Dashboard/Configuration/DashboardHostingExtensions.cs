using System.Net;
using Microsoft.Extensions.Options;

namespace PcMonitor.Dashboard.Configuration;

internal static class DashboardHostingExtensions
{
    /// <summary>
    /// Registers validated <see cref="DashboardOptions"/> and binds Kestrel to <c>127.0.0.1</c> only.
    /// An explicit <c>Listen</c> overrides <c>ASPNETCORE_URLS</c> and <c>--urls</c>, so the dashboard
    /// can never be exposed to the network through configuration.
    /// </summary>
    public static WebApplicationBuilder AddLoopbackDashboard(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<DashboardOptions>()
            .BindConfiguration(DashboardOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.WebHost.ConfigureKestrel((context, kestrel) =>
        {
            var options = kestrel.ApplicationServices.GetRequiredService<IOptions<DashboardOptions>>().Value;
            kestrel.Listen(IPAddress.Loopback, options.Port);
        });

        return builder;
    }
}
