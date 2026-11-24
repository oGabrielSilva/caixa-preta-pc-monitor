using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using PcMonitor.Application.Live;
using PcMonitor.Dashboard.Api.Sensors;

namespace PcMonitor.Dashboard.Api.Live;

internal static class LiveEndpoints
{
    /// <summary>How much recent recording the live page shows.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public static RouteGroupBuilder MapLiveEndpoints(this RouteGroupBuilder api)
    {
        var live = api.MapGroup("/live").WithTags("Live").RequireStorage();

        live.MapGet("/", (ILiveReader reader, TimeProvider time) => TypedResults.Ok(Snapshot(reader, time)))
            .WithName("GetLive");

        live.MapGet("/stream", (long? segmentId, long? after, LiveFeed feed, IHostApplicationLifetime lifetime, HttpContext context) =>
                TypedResults.ServerSentEvents(EventsAsync(feed, segmentId, after, context.RequestAborted, lifetime.ApplicationStopping)))
            .WithName("StreamLive")
            .WithDescription("Server-sent events (`frames`, `segment`, `state`) after the frame `after` (Unix ms) of segment `segmentId`.")
            .Produces<LiveEvent>(StatusCodes.Status200OK, "text/event-stream");

        return api;
    }

    private static LiveResponse Snapshot(ILiveReader reader, TimeProvider time)
    {
        var snapshot = reader.Latest(Window);
        if (snapshot is null)
        {
            return new LiveResponse(LiveState.NoData, null, null, [], []);
        }

        var lastFrameAt = snapshot.Frames.Count > 0 ? snapshot.Frames[^1].Timestamp : (DateTimeOffset?)null;
        return new LiveResponse(
            LiveStateRule.Of(snapshot.Session, lastFrameAt, time.GetUtcNow()),
            new SessionResponse(snapshot.Session.StartedAt, snapshot.Session.SourceStatus, snapshot.Session.BoardProduct),
            snapshot.SegmentId,
            [.. snapshot.Catalog.Sensors.Select(SensorPresentation.Describe)],
            [.. snapshot.Frames.Select(FramePoint.From)]);
    }

    /// <summary>
    /// Ends when the client leaves or when the service stops: a stream never ends by itself, and the host would otherwise
    /// wait its whole shutdown timeout for it (a stuck <c>Stop-Service</c> while a tab is open).
    /// </summary>
    private static async IAsyncEnumerable<SseItem<LiveEvent>> EventsAsync(
        LiveFeed feed, long? segmentId, long? after, [EnumeratorCancellation] CancellationToken requestAborted, CancellationToken stopping)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(requestAborted, stopping);
        var since = after is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : (DateTimeOffset?)null;
        await foreach (var update in feed.FollowAsync(segmentId, since, lifetime.Token).ConfigureAwait(false))
        {
            var item = LiveEvent.From(update);
            yield return new SseItem<LiveEvent>(item, item.EventType);
        }
    }
}
