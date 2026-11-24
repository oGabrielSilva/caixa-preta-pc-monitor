using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcMonitor.Collector.Hosting;
using PcMonitor.Collector.Retention;
using PcMonitor.Collector.Sampling;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class CollectorCompositionTests
{
    [Fact]
    public void Sampling_stops_before_retention_so_the_shutdown_reason_is_written_first()
    {
        // The host stops hosted services in reverse registration order.
        var hosted = new ServiceCollection().AddCollector()
            .Where(d => d.ServiceType == typeof(IHostedService))
            .Select(d => d.ImplementationType)
            .ToList();

        hosted.IndexOf(typeof(SamplingWorker)).ShouldBeGreaterThan(hosted.IndexOf(typeof(RetentionWorker)));
    }
}
