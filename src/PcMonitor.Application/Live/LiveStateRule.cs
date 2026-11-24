namespace PcMonitor.Application.Live;

public static class LiveStateRule
{
    /// <summary>A frame older than this means recording is not live (five missed one-second samples).</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(5);

    public static LiveState Of(SessionView? session, DateTimeOffset? lastFrameAt, DateTimeOffset now) => session switch
    {
        null => LiveState.NoData,
        { EndedAt: not null } => LiveState.Stopped,
        _ when lastFrameAt is { } last && now - last <= StaleAfter => LiveState.Recording,
        _ => LiveState.Stale,
    };
}
