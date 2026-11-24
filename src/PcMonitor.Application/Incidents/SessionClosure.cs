using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Application.Incidents;

/// <summary>How and when to end an open session (and its open segment).</summary>
public sealed record SessionClosure(OpenSession Session, DateTimeOffset At, EndReason Reason);
