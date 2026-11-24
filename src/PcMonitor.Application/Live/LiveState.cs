namespace PcMonitor.Application.Live;

public enum LiveState
{
    /// <summary>Nothing was ever recorded.</summary>
    NoData,

    /// <summary>The collector is writing frames now.</summary>
    Recording,

    /// <summary>The session is open but frames stopped arriving (collector stuck, or the PC just resumed or lost power).</summary>
    Stale,

    /// <summary>The last session ended: the collector was stopped or the PC shut down.</summary>
    Stopped,
}
