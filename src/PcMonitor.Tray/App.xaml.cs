using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using PcMonitor.Tray.Settings;
using PcMonitor.Tray.Startup;
using PcMonitor.Tray.Tray;
using PcMonitor.Tray.Windows;

namespace PcMonitor.Tray;

/// <summary>
/// The tray app: one instance per session, the tray icon, the always-on-top panel and, on demand, the main window.
/// Started with <c>--background</c> (at login) it shows no main window.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "WPF owns the Application's lifetime: the tray icon and the instance lock are released in OnExit.")]
public partial class App
{
    private const string InstanceName = "CaixaPreta.Tray";

    private SingleInstance? _instance;
    private TrayIcon? _tray;
    private TraySettings _settings = new();
    private CoreWebView2Environment? _environment;
    private Uri _dashboard = new("http://127.0.0.1:5280/");
    private MainWindow? _main;
    private MiniWindow? _mini;

    internal bool IsExiting { get; private set; }

    private async void OnStartupAsync(object sender, StartupEventArgs e)
    {
        _instance = SingleInstance.TryAcquire(InstanceName);
        if (_instance is null)
        {
            SingleInstance.SignalExisting(InstanceName);
            Shutdown();
            return;
        }

        _settings = TraySettings.Load(TraySettings.DefaultPath);
        _dashboard = DashboardAddress.FromArguments(e.Args, DashboardAddress.DefaultFolder);
        var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaixaPreta", "WebView2");
        _environment = await CoreWebView2Environment.CreateAsync(null, userData).ConfigureAwait(true);

        var runAtLogin = new RunAtLogin(RunAtLogin.CurrentUserRunKey, Environment.ProcessPath!);
        if (!_settings.RunAtLoginOffered)
        {
            runAtLogin.IsEnabled = true;
            _settings.RunAtLoginOffered = true;
            _settings.Save(TraySettings.DefaultPath);
        }

        _tray = new TrayIcon(TrayText.ForCurrentCulture(), runAtLogin, _settings.ShowMiniPanel)
        {
            OpenRequested = () => ShowMain(null),
            MiniPanelToggled = ToggleMini,
            ExitRequested = ExitApp,
        };

        if (_settings.ShowMiniPanel)
        {
            ShowMini();
        }

        if (!e.Args.Contains("--background"))
        {
            ShowMain(null);
        }

        // Only now can the main window be built: a second launch during startup waits on the pipe until here.
        _instance.Listen(() => Dispatcher.Invoke(() => ShowMain(null)));
    }

    /// <summary>Shows the main window, optionally on a dashboard page (from the mini panel or a second instance).</summary>
    internal void ShowMain(string? path)
    {
        _main ??= new MainWindow(_environment!, _dashboard, this);
        _main.ShowOnAsync(path);
    }

    private void ShowMini()
    {
        _mini ??= new MiniWindow(_environment!, _dashboard, _settings, this);
        _mini.Show();
    }

    private void ToggleMini(bool visible)
    {
        _settings.ShowMiniPanel = visible;
        _settings.Save(TraySettings.DefaultPath);
        if (visible)
        {
            ShowMini();
        }
        else
        {
            _mini?.Hide();
        }
    }

    private void ExitApp()
    {
        IsExiting = true;
        Shutdown();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _tray?.Dispose();
        _instance?.Dispose();
    }
}
