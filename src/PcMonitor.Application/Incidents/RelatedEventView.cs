namespace PcMonitor.Application.Incidents;

/// <summary>
/// A Windows event around an incident, without its data: event payloads can name the computer and the user, so they
/// never leave the database.
/// </summary>
public sealed record RelatedEventView(string Provider, int EventId, DateTimeOffset LoggedAt, int Level);
