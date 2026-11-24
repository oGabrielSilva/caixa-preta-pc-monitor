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

        live.MapGet("/stream", (long? segmentId, long? after, LiveFeed feed, HttpContext context) =>
                TypedResults.ServerSentEvents(EventsAsync(feed, segmentId, after, context.RequestAborted)))
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

    private static async IAsyncEnumerable<SseItem<LiveEvent>> EventsAsync(
        LiveFeed feed, long? segmentId, long? after, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var since = after is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : (DateTimeOffset?)null;
        await foreach (var update in feed.FollowAsync(segmentId, since, cancellationToken).ConfigureAwait(false))
        {
            var item = LiveEvent.From(update);
            yield return new SseItem<LiveEvent>(item, item.EventType);
        }
    }
}
