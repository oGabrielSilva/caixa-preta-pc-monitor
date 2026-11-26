using System.Drawing;
using PcMonitor.Tray.Startup;
using Forms = System.Windows.Forms;

namespace PcMonitor.Tray.Tray;

/// <summary>The tray icon: double click opens the dashboard; the menu toggles the panel and the start with Windows.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Icon _image;

    public TrayIcon(TrayText text, RunAtLogin runAtLogin, bool miniPanelShown)
    {
        using var stream = typeof(TrayIcon).Assembly.GetManifestResourceStream("CaixaPreta.ico")!;
        _image = new Icon(stream);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(text.Open, null, (_, _) => OpenRequested?.Invoke());
        var mini = new Forms.ToolStripMenuItem(text.ShowMiniPanel) { CheckOnClick = true, Checked = miniPanelShown };
        mini.CheckedChanged += (_, _) => MiniPanelToggled?.Invoke(mini.Checked);
        menu.Items.Add(mini);
        var login = new Forms.ToolStripMenuItem(text.RunAtLogin) { CheckOnClick = true, Checked = runAtLogin.IsEnabled };
        login.CheckedChanged += (_, _) => runAtLogin.IsEnabled = login.Checked;
        menu.Items.Add(login);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(text.Exit, null, (_, _) => ExitRequested?.Invoke());

        _icon = new Forms.NotifyIcon { Icon = _image, Text = text.Tooltip, ContextMenuStrip = menu, Visible = true };
        _icon.DoubleClick += (_, _) => OpenRequested?.Invoke();
    }

    public Action? OpenRequested { get; init; }

    public Action<bool>? MiniPanelToggled { get; init; }

    public Action? ExitRequested { get; init; }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _image.Dispose();
    }
}
