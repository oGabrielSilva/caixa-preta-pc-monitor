using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Diagnosis;
using PcMonitor.Application.Incidents;
using PcMonitor.Collector.Hosting;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Diagnosis.Rules;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Collector.Tests.Hosting;

public sealed class StartupAnalysisTests
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

    private sealed class OneIncident : IDiagnosisInputReader, IDiagnosisWriter
    {
        public List<long> Saved { get; } = [];

        public IReadOnlyList<IncidentEvidence> IncidentsNeedingDiagnosis(int engineVersion) =>
        [
            new(1, new Incident(
                new UnexpectedShutdown(1, DateTimeOffset.UnixEpoch, 0, 0, null, false, null, new Dictionary<string, string>()),
                null, FailureTimeSource.Unknown, null, null, null, []), null),
        ];

        public void Save(long incidentId, IncidentDiagnosis diagnosis) => Saved.Add(incidentId);
    }

    private sealed class BrokenDiagnosisInput : IDiagnosisInputReader
    {
        public IReadOnlyList<IncidentEvidence> IncidentsNeedingDiagnosis(int engineVersion) => throw new IOException("database busy");
    }

    private static StartupAnalysis Analysis(ISystemEventLog eventLog, IDiagnosisInputReader inputs, IDiagnosisWriter writer)
    {
        var store = new EmptyStore();
        return new StartupAnalysis(
            new DetectIncidents(eventLog, store, store, NullLogger<DetectIncidents>.Instance),
            new DiagnoseIncidents(inputs, writer, new DiagnosisEngine(DiagnosticRules.All), NullLogger<DiagnoseIncidents>.Instance),
            NullLogger<StartupAnalysis>.Instance);
    }

    [Fact]
    public void Diagnosis_still_runs_when_detection_fails()
    {
        var diagnoses = new OneIncident();

        var result = Analysis(new BrokenEventLog(), diagnoses, diagnoses).Run();

        result.Detection.ShouldBeNull();
        result.Diagnosed.ShouldBe(1);
        diagnoses.Saved.ShouldBe([1L]);
    }

    [Fact]
    public void A_failing_diagnosis_is_logged_and_does_not_throw()
    {
        var result = Analysis(new BrokenEventLog(), new BrokenDiagnosisInput(), new OneIncident()).Run();

        result.Diagnosed.ShouldBeNull();
    }

    [Fact]
    public void Collector_registers_incident_detection_and_diagnosis_services()
    {
        var services = new ServiceCollection().AddCollector().Select(d => d.ServiceType).ToList();

        services.ShouldContain(typeof(ISystemEventLog));
        services.ShouldContain(typeof(IIncidentReader));
        services.ShouldContain(typeof(IIncidentWriter));
        services.ShouldContain(typeof(IDiagnosisInputReader));
        services.ShouldContain(typeof(IDiagnosisWriter));
        services.ShouldContain(typeof(DiagnosisEngine));
        services.ShouldContain(typeof(StartupAnalysis));
    }
}
