namespace PcMonitor.Dashboard.Api.History;

/// <summary>One point of the history chart: <c>t</c> (Unix ms) is the start of the bucket.</summary>
public sealed record BucketResponse(long T, double Min, double Avg, double Max);
