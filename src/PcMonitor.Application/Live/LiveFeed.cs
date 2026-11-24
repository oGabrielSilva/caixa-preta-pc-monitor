using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace PcMonitor.Application.Live;

/// <summary>Turns a <see cref="LiveCursor"/> into a stream: polls the database every second until cancelled.</summary>
public sealed partial class LiveFeed(ILiveReader reader, TimeProvider time, ILogger<LiveFeed> logger)
{
    /// <summary>How often the database is read: the collector writes one frame per second.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public async IAsyncEnumerable<LiveUpdate> FollowAsync(
        long? segmentId,
        DateTimeOffset? after,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cursor = new LiveCursor(reader, time, segmentId, after ?? DateTimeOffset.MinValue);
        while (await WaitAsync(cancellationToken).ConfigureAwait(false))
        {
            IReadOnlyList<LiveUpdate> updates;
            try
            {
                updates = cursor.Poll();
            }
#pragma warning disable CA1031 // A busy or briefly unreadable database must not end the client's stream; the next poll retries.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogPollFailed(exception);
                continue;
            }

            foreach (var update in updates)
            {
                yield return update;
            }
        }
    }

    private async Task<bool> WaitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(PollInterval, time, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reading live frames failed; retrying on the next poll")]
    private partial void LogPollFailed(Exception exception);
}
