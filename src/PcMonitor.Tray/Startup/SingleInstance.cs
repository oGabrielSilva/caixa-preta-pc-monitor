using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace PcMonitor.Tray.Startup;

/// <summary>
/// Keeps one tray app per user session. The first instance owns a named mutex and, once it is ready, listens on a
/// named pipe; a second instance signals it (to show its window) and exits.
/// </summary>
/// <remarks>
/// The mutex is per session (<c>Local\</c>), but pipe names are machine-wide, so the pipe name carries the session id:
/// two users signed in at once each get their own.
/// </remarks>
internal sealed class SingleInstance : IDisposable
{
    /// <summary>How long a second instance waits for the first one; it may still be starting (at login).</summary>
    private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(15);

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(Mutex mutex, string name)
    {
        _mutex = mutex;
        _pipeName = PipeName(name, CurrentSession);
    }

    private static int CurrentSession => Process.GetCurrentProcess().SessionId;

    /// <summary>The pipe of <paramref name="name"/> in one Windows session.</summary>
    public static string PipeName(string name, int sessionId) => $"{name}.{sessionId}";

    /// <summary>Becomes the instance, or returns <c>null</c> when another one already runs.</summary>
    public static SingleInstance? TryAcquire(string name)
    {
        var mutex = new Mutex(initiallyOwned: true, $@"Local\{name}", out var created);
        if (!created)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstance(mutex, name);
    }

    /// <summary>
    /// Starts answering second instances. Call it when the app can show its window: a request sent before waits on
    /// the pipe until then.
    /// </summary>
    public void Listen(Action onShowRequested) => _ = ListenAsync(onShowRequested);

    /// <summary>Asks the running instance to show its window.</summary>
    public static void SignalExisting(string name)
    {
        using var client = new NamedPipeClientStream(".", PipeName(name, CurrentSession), PipeDirection.Out);
        try
        {
            client.Connect(_connectTimeout);
            client.WriteByte(1);
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or UnauthorizedAccessException)
        {
            // The other instance is closing or not answering; nothing to show.
        }
    }

    private async Task ListenAsync(Action onShowRequested)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                onShowRequested();
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
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Disposed from another thread than the one that took it: closing the handle (or the process) frees it.
        }

        _mutex.Dispose();
    }
}
