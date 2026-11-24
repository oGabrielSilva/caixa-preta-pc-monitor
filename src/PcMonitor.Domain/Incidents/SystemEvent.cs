namespace PcMonitor.Domain.Incidents;

/// <summary>An entry of the Windows event log.</summary>
/// <param name="Provider">Event provider, e.g. <c>Microsoft-Windows-Kernel-Power</c>.</param>
/// <param name="EventId">Event id, e.g. 41.</param>
/// <param name="RecordId">Position of the entry in its log.</param>
/// <param name="LoggedAt">When Windows wrote the entry.</param>
/// <param name="Level">1 critical, 2 error, 3 warning, 4 information.</param>
/// <param name="Data">Event data by name (or by position for unnamed data; the binary part under <c>Binary</c>).</param>
public sealed record SystemEvent(
    string Provider, int EventId, long RecordId, DateTimeOffset LoggedAt, int Level, IReadOnlyDictionary<string, string> Data);
