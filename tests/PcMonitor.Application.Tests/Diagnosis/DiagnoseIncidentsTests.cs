using Microsoft.Extensions.Logging.Abstractions;
using PcMonitor.Application.Diagnosis;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Application.Tests.Diagnosis;

public sealed class DiagnoseIncidentsTests
{
    private static readonly DateTimeOffset _at = new(2026, 11, 24, 13, 54, 38, TimeSpan.Zero);

    private sealed class FakeInputs(params IncidentEvidence[] inputs) : IDiagnosisInputReader
    {
        public List<int> RequestedVersions { get; } = [];

        public IReadOnlyList<IncidentEvidence> IncidentsNeedingDiagnosis(int engineVersion)
        {
            RequestedVersions.Add(engineVersion);
            return inputs;
        }
    }

    private sealed class RecordingWriter : IDiagnosisWriter
    {
        public Dictionary<long, IncidentDiagnosis> Saved { get; } = [];

        public long? FailOn { get; set; }

        public void Save(long incidentId, IncidentDiagnosis diagnosis)
        {
            if (incidentId == FailOn)
            {
                throw new IOException("database busy");
            }

            Saved[incidentId] = diagnosis;
        }
    }

    private sealed class SupplyRule : IDiagnosticRule
    {
        public IEnumerable<Finding> Evaluate(IncidentEvidence evidence) =>
            [Finding.For("test", Hypothesis.PowerSupply, EvidenceStrength.Moderate)];
    }

    private static IncidentEvidence Input(long id) => new(
        id,
        new Incident(
            new UnexpectedShutdown(id, _at, 0, 0, null, false, null, new Dictionary<string, string>()),
            null, FailureTimeSource.Unknown, null, null, null, []),
        Telemetry: null);

    private static int Run(FakeInputs inputs, RecordingWriter writer) =>
        new DiagnoseIncidents(inputs, writer, new DiagnosisEngine([new SupplyRule()]), NullLogger<DiagnoseIncidents>.Instance).Run();

    [Fact]
    public void Each_incident_needing_diagnosis_is_diagnosed_and_saved()
    {
        var writer = new RecordingWriter();

        Run(new FakeInputs(Input(1), Input(2)), writer).ShouldBe(2);

        writer.Saved.Keys.ShouldBe([1L, 2L], ignoreOrder: true);
        writer.Saved[1].Top.ShouldBe(Hypothesis.PowerSupply);
    }

    [Fact]
    public void Reader_is_asked_for_the_current_engine_version()
    {
        var inputs = new FakeInputs();

        Run(inputs, new RecordingWriter());

        inputs.RequestedVersions.ShouldBe([DiagnosisEngine.Version]);
    }

    [Fact]
    public void Incident_without_telemetry_gets_a_diagnosis_with_the_missing_telemetry_limitation()
    {
        var writer = new RecordingWriter();

        Run(new FakeInputs(Input(1)), writer);

        writer.Saved[1].Limitations.Select(l => l.Code).ShouldContain("telemetry.missing");
    }

    [Fact]
    public void A_failing_incident_does_not_stop_the_others()
    {
        var writer = new RecordingWriter { FailOn = 1 };

        Run(new FakeInputs(Input(1), Input(2)), writer).ShouldBe(1);

        writer.Saved.Keys.ShouldBe([2L]);
    }
}
