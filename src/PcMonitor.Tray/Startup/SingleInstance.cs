using System.IO;
using System.IO.Pipes;

namespace PcMonitor.Tray.Startup;

/// <summary>
/// Keeps one tray app per user session. The first instance owns a named mutex and listens on a named pipe; a second
/// instance signals it (to show its window) and exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly Action _onShowRequested;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(Mutex mutex, string name, Action onShowRequested)
    {
        _mutex = mutex;
        _pipeName = name;
        _onShowRequested = onShowRequested;
        _ = ListenAsync();
    }

    /// <summary>Becomes the instance, or returns <c>null</c> when another one already runs.</summary>
    public static SingleInstance? TryAcquire(string name, Action onShowRequested)
    {
        var mutex = new Mutex(initiallyOwned: true, $@"Local\{name}", out var created);
        if (!created)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, name, onShowRequested);
    }

    /// <summary>Asks the running instance to show its window.</summary>
    public static void SignalExisting(string name)
    {
        using var client = new NamedPipeClientStream(".", name, PipeDirection.Out);
        try
        {
            client.Connect(TimeSpan.FromSeconds(2));
            client.WriteByte(1);
        }
        catch (Exception exception) when (exception is TimeoutException or IOException)
        {
            // The other instance is closing; nothing to show.
        }
    }

    private async Task ListenAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                _onShowRequested();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
                // A client gave up mid-connection: listen again.
            }
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
