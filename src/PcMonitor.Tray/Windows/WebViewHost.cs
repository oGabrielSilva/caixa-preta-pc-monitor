using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace PcMonitor.Tray.Windows;

/// <summary>
/// Sets up a WebView2 showing a dashboard page, for both windows: shared environment, no developer tools or context
/// menu, external links in the default browser, a local "waiting" page with a retry every 5 s while the dashboard does
/// not answer, and the messages pages post (<c>open</c>, <c>drag</c>, <c>resize</c>, <c>scale</c>, <c>size</c>).
/// </summary>
internal sealed class WebViewHost
{
    private static readonly TimeSpan _retry = TimeSpan.FromSeconds(5);
    private static readonly Lazy<string> _offlinePage = new(() =>
    {
        using var stream = typeof(WebViewHost).Assembly.GetManifestResourceStream("CaixaPreta.offline.html")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    private readonly WebView2 _view;
    private readonly Uri _dashboard;
    private readonly DispatcherTimer _retryTimer;
    private Uri _target;

    public WebViewHost(WebView2 view, Uri dashboard, string path)
    {
        _view = view;
        _dashboard = dashboard;
        _target = new Uri(dashboard, path);
        _retryTimer = new DispatcherTimer { Interval = _retry };
        _retryTimer.Tick += (_, _) =>
        {
            _retryTimer.Stop();
            _view.CoreWebView2?.Navigate(_target.ToString());
        };
    }

    /// <summary>A page asked to open the main window on <c>path</c>.</summary>
    public Action<string>? OpenRequested { get; init; }

    /// <summary>The mini panel's drag handle was pressed.</summary>
    public Action? DragRequested { get; init; }

    /// <summary>The mini panel's resize grip was pressed.</summary>
    public Action? ResizeRequested { get; init; }

    /// <summary>Ctrl + mouse wheel over the mini panel: +1 grows, -1 shrinks.</summary>
    public Action<int>? ScaleStepRequested { get; init; }

    /// <summary>The page measured its content (CSS pixels at zoom 1).</summary>
    public Action<System.Windows.Size>? SizeReported { get; init; }

    public async Task InitializeAsync(CoreWebView2Environment environment)
    {
        await _view.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
        var core = _view.CoreWebView2;
        core.Settings.AreDevToolsEnabled = Debugger.IsAttached;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        // Ctrl + wheel and Ctrl + +/- would zoom the page alone; the mini panel asks the host to scale the window instead.
        core.Settings.IsZoomControlEnabled = false;
        core.NavigationCompleted += (_, e) =>
        {
            if (!e.IsSuccess && e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
            {
                core.NavigateToString(_offlinePage.Value);
                _retryTimer.Start();
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            {
                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            }
        };
        core.WebMessageReceived += (_, e) => OnMessage(e.WebMessageAsJson);
        core.Navigate(_target.ToString());
    }

    public void Navigate(string path)
    {
        _target = new Uri(_dashboard, path);
        _view.CoreWebView2?.Navigate(_target.ToString());
    }

    private void OnMessage(string json)
    {
        using var document = JsonDocument.Parse(json);
        var message = document.RootElement;
        if (message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("type", out var type))
        {
            return;
        }

        switch (type.GetString())
        {
            case "open":
                var path = message.TryGetProperty("path", out var value) ? value.GetString() : null;
                OpenRequested?.Invoke(path is { Length: > 0 } && path[0] == '/' ? path : "/");
                break;
            case "drag":
                DragRequested?.Invoke();
                break;
            case "resize":
                ResizeRequested?.Invoke();
                break;
            case "scale":
                if (message.TryGetProperty("step", out var step) && step.TryGetInt32(out var steps) && steps != 0)
                {
                    ScaleStepRequested?.Invoke(Math.Sign(steps));
                }

                break;
            case "size":
                if (message.TryGetProperty("width", out var width) && width.TryGetDouble(out var w)
                    && message.TryGetProperty("height", out var height) && height.TryGetDouble(out var h) && w > 0 && h > 0)
                {
                    SizeReported?.Invoke(new System.Windows.Size(w, h));
                }

                break;
        }
    }
}
