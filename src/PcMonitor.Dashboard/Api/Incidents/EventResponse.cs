namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>A Windows event around the incident. Its data is never sent: it can name the computer and the user.</summary>
public sealed record EventResponse(string Provider, int EventId, DateTimeOffset LoggedAt, int Level);
