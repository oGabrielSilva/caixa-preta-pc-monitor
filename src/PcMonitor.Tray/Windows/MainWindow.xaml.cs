using System.ComponentModel;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace PcMonitor.Tray.Windows;

/// <summary>The full dashboard. Closing it only hides it; the app stays in the tray.</summary>
public partial class MainWindow
{
    private readonly WebViewHost _host;
    private readonly Task _ready;

    internal MainWindow(CoreWebView2Environment environment, Uri dashboard, App app)
    {
        InitializeComponent();
        _host = new WebViewHost(View, dashboard, "/");
        _ready = _host.InitializeAsync(environment);
        Closing += (_, e) => OnClosing(app, e);
    }

    /// <summary>Shows and focuses the window, on <paramref name="path"/> when given.</summary>
    internal async void ShowOnAsync(string? path)
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        // Windows refuses focus to a process in the background; briefly topmost brings the window to the front anyway.
        Topmost = true;
        Activate();
        Topmost = false;
        await _ready.ConfigureAwait(true);
        if (path is not null)
        {
            _host.Navigate(path);
        }
    }

    private void OnClosing(App app, CancelEventArgs e)
    {
        if (!app.IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
