using PcMonitor.Dashboard.Api.Sensors;
using PcMonitor.Domain.Diagnosis;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>The recorded seconds before the end of telemetry, by sensor role.</summary>
/// <param name="EndAt">Last recorded frame.</param>
/// <param name="AtFailure">The window ends at the failure; otherwise the collector had stopped or the machine was asleep.</param>
/// <param name="Series">One series per recorded role; readings without a value are left out.</param>
public sealed record TelemetryResponse(DateTimeOffset EndAt, bool AtFailure, IReadOnlyList<RoleSeriesResponse> Series)
{
    public static TelemetryResponse From(TelemetryWindow window, bool atFailure) => new(
        window.End,
        atFailure,
        [.. Enum.GetValues<SensorRole>().Where(window.Has).Select(role => RoleSeriesResponse.From(role, window.Series(role)))]);
}

/// <summary>Readings of one sensor role, with how to judge them.</summary>
public sealed record RoleSeriesResponse(
    SensorRole Role,
    string Unit,
    RangeResponse? Range,
    float? Limit,
    float? Critical,
    IReadOnlyList<PointResponse> Points)
{
    public static RoleSeriesResponse From(SensorRole role, IReadOnlyList<(DateTimeOffset At, float Value)> points) => new(
        role,
        SensorPresentation.UnitOf(role),
        SensorPresentation.RangeOf(role),
        SensorPresentation.LimitOf(role),
        SensorPresentation.CriticalOf(role),
        [.. points.Select(p => new PointResponse(p.At.ToUnixTimeMilliseconds(), p.Value))]);
}
