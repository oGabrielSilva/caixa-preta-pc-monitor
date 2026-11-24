using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Incidents;
using PcMonitor.Collector.Hosting;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class StartupIncidentDetectionTests
{
    private sealed class BrokenEventLog : ISystemEventLog
    {
        public IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns(DateTimeOffset? after) =>
            throw new UnauthorizedAccessException("event log unavailable");

        public IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until) => [];
    }

    private sealed class EmptyStore : IIncidentStore
    {
        public DateTimeOffset? LatestIncidentLoggedAt() => null;

        public IReadOnlyList<OpenSession> OpenSessions() => [];

        public bool SaveIncident(Incident incident) => true;

        public void EndSession(OpenSession session, DateTimeOffset at, EndReason reason)
        {
        }
    }

    [Fact]
    public void Startup_detection_logs_and_continues_when_detection_throws()
    {
        var detection = new StartupIncidentDetection(
            new DetectIncidents(new BrokenEventLog(), new EmptyStore(), NullLogger<DetectIncidents>.Instance),
            NullLogger<StartupIncidentDetection>.Instance);

        detection.Run().ShouldBeNull();
    }

    [Fact]
    public void Collector_registers_incident_detection_services()
    {
        var services = new ServiceCollection().AddCollector().Select(d => d.ServiceType).ToList();

        services.ShouldContain(typeof(ISystemEventLog));
        services.ShouldContain(typeof(IIncidentStore));
        services.ShouldContain(typeof(DetectIncidents));
        services.ShouldContain(typeof(StartupIncidentDetection));
    }
}
