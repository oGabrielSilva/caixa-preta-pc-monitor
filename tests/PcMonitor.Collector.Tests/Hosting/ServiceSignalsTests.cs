using Microsoft.Extensions.Time.Testing;
using PcMonitor.Collector.Hosting;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class ServiceSignalsTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 24, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Stop_reason_defaults_to_ServiceStopped() => new ServiceSignals(_time).StopReason.ShouldBe(EndReason.ServiceStopped);

    [Fact]
    public void Stop_reason_is_SystemShutdown_after_a_shutdown_signal()
    {
        var signals = new ServiceSignals(_time);

        signals.Enqueue(ServiceSignal.Shutdown);

        signals.StopReason.ShouldBe(EndReason.SystemShutdown);
    }

    [Fact]
    public void Signals_are_dequeued_in_order_with_the_time_they_arrived()
    {
        var signals = new ServiceSignals(_time);
        signals.Enqueue(ServiceSignal.Suspend);
        _time.Advance(TimeSpan.FromSeconds(1));
        signals.Enqueue(ServiceSignal.Resume);

        signals.TryDequeue(out var first).ShouldBeTrue();
        signals.TryDequeue(out var second).ShouldBeTrue();
        signals.TryDequeue(out _).ShouldBeFalse();
        first.ShouldBe(new ReceivedSignal(ServiceSignal.Suspend, _time.GetUtcNow().AddSeconds(-1)));
        second.ShouldBe(new ReceivedSignal(ServiceSignal.Resume, _time.GetUtcNow()));
    }
}
