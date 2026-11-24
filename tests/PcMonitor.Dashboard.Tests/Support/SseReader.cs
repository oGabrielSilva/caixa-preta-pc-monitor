using System.Text.Json;

namespace PcMonitor.Dashboard.Tests.Support;

/// <summary>Reads server-sent events from an open response, one <c>event:</c>/<c>data:</c> pair at a time.</summary>
public sealed class SseReader(Stream stream) : IDisposable
{
    private readonly StreamReader _reader = new(stream);

    /// <summary>Skips events until one of <paramref name="type"/> arrives, then returns its data; fails after the timeout.</summary>
    public async Task<JsonElement> Next(string type, TimeSpan timeout)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(timeout);
        string? eventType = null;
        while (await _reader.ReadLineAsync(cancellation.Token) is { } line)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line["event: ".Length..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal) && eventType == type)
            {
                using var document = JsonDocument.Parse(line["data: ".Length..]);
                return document.RootElement.Clone();
            }
        }

        throw new EndOfStreamException($"The stream ended before a '{type}' event.");
    }

    /// <summary>Reads until the server ends the stream; <c>false</c> when it is still open after the timeout.</summary>
    public async Task<bool> EndsWithin(TimeSpan timeout)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(timeout);
        try
        {
            while (await _reader.ReadLineAsync(cancellation.Token) is not null)
            {
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose() => _reader.Dispose();
}
