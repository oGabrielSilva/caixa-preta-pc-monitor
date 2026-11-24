using PcMonitor.Collector.Hosting;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class ServiceSignalsTests
{
    [Fact]
    public void Stop_reason_defaults_to_ServiceStopped() => new ServiceSignals().StopReason.ShouldBe(EndReason.ServiceStopped);

    [Fact]
    public void Stop_reason_is_SystemShutdown_after_a_shutdown_signal()
    {
        var signals = new ServiceSignals();

        signals.Enqueue(ServiceSignal.Shutdown);

        signals.StopReason.ShouldBe(EndReason.SystemShutdown);
    }

    [Fact]
    public void Signals_are_dequeued_in_order()
    {
        var signals = new ServiceSignals();
        signals.Enqueue(ServiceSignal.Suspend);
        signals.Enqueue(ServiceSignal.Resume);

        signals.TryDequeue(out var first).ShouldBeTrue();
        signals.TryDequeue(out var second).ShouldBeTrue();
        signals.TryDequeue(out _).ShouldBeFalse();
        (first, second).ShouldBe((ServiceSignal.Suspend, ServiceSignal.Resume));
    }
}
