using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Incidents;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Domain.Tests.Diagnosis.Support;

/// <summary>Builds diagnosis inputs: a crash, optional telemetry series per role and related events.</summary>
internal sealed class EvidenceBuilder
{
    public static readonly DateTimeOffset End = new(2026, 11, 24, 13, 50, 30, TimeSpan.Zero);

    private readonly Dictionary<SensorRole, IReadOnlyList<(DateTimeOffset At, float Value)>> _series = [];
    private readonly List<SystemEvent> _events = [];
    private bool _withTelemetry;

    public long BugcheckCode { get; set; }

    public int SleepState { get; set; }

    public bool LongPowerButtonPress { get; set; }

    public DateTimeOffset? PowerButtonPressedAt { get; set; }

    public DateTimeOffset LoggedAt { get; set; } = End.AddMinutes(4);

    public DateTimeOffset? LastAliveAt { get; set; }

    public FailureTimeSource Source { get; set; } = FailureTimeSource.LastFrame;

    public Dictionary<string, string> Data { get; } = [];

    /// <summary>One value per second ending at <see cref="End"/>, oldest first.</summary>
    public EvidenceBuilder Series(SensorRole role, params float[] values)
    {
        _withTelemetry = true;
        _series[role] = [.. values.Select((v, i) => (End.AddSeconds(i - values.Length + 1), v))];
        return this;
    }

    /// <summary>A constant series of <paramref name="count"/> values.</summary>
    public EvidenceBuilder Constant(SensorRole role, float value, int count = 120) =>
        Series(role, [.. Enumerable.Repeat(value, count)]);

    public EvidenceBuilder WithTelemetry()
    {
        _withTelemetry = true;
        return this;
    }

    public EvidenceBuilder Event(string provider, int id, DateTimeOffset at, Dictionary<string, string>? data = null)
    {
        _events.Add(new SystemEvent(provider, id, _events.Count + 1, at, 2, data ?? []));
        return this;
    }

    public IncidentEvidence Build()
    {
        var shutdown = new UnexpectedShutdown(1, LoggedAt, BugcheckCode, SleepState, PowerButtonPressedAt, LongPowerButtonPress, LastAliveAt, Data);
        var estimate = Source switch
        {
            FailureTimeSource.LastFrame or FailureTimeSource.LastFrameBeforeSleep => End,
            FailureTimeSource.LastAliveEventLog => LastAliveAt,
            _ => (DateTimeOffset?)null,
        };
        var incident = new Incident(shutdown, estimate, Source, _withTelemetry ? 1 : null, _withTelemetry ? 1 : null,
            _withTelemetry ? End : null, _events);
        return new IncidentEvidence(7, incident, _withTelemetry ? new TelemetryWindow(End, _series) : null);
    }
}
