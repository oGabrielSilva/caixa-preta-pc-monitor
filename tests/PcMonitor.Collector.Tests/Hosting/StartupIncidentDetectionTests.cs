using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Incidents;
using PcMonitor.Collector.Hosting;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class StartupIncidentDetectionTests
{
    private sealed class BrokenEventLog : ISystemEventLog
    {
        public IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns() =>
            throw new UnauthorizedAccessException("event log unavailable");

        public IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset since, DateTimeOffset until) => [];
    }

    private sealed class EmptyStore : IIncidentReader, IIncidentWriter
    {
        public IReadOnlySet<IncidentKey> SavedIncidents() => new HashSet<IncidentKey>();

        public IReadOnlyList<OpenSession> OpenSessions() => [];

        public bool SaveIncident(Incident incident, SessionClosure? closure) => true;

        public void CloseSession(SessionClosure closure)
        {
        }
    }

    [Fact]
    public void Startup_detection_logs_and_continues_when_detection_throws()
    {
        var detection = new StartupIncidentDetection(
            new DetectIncidents(new BrokenEventLog(), new EmptyStore(), new EmptyStore(), NullLogger<DetectIncidents>.Instance),
            NullLogger<StartupIncidentDetection>.Instance);

        detection.Run().ShouldBeNull();
    }

    [Fact]
    public void Collector_registers_incident_detection_services()
    {
        var services = new ServiceCollection().AddCollector().Select(d => d.ServiceType).ToList();

        services.ShouldContain(typeof(ISystemEventLog));
        services.ShouldContain(typeof(IIncidentReader));
        services.ShouldContain(typeof(IIncidentWriter));
        services.ShouldContain(typeof(DetectIncidents));
        services.ShouldContain(typeof(StartupIncidentDetection));
    }
}
