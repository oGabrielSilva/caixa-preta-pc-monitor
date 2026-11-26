using System.Windows;
using PcMonitor.Tray.Settings;

namespace PcMonitor.Tray.Tests;

public sealed class TraySettingsTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("caixa-preta-settings-").FullName;

    private string File => Path.Combine(_folder, "settings.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Settings_default_to_the_mini_panel_shown_without_a_position()
    {
        var settings = TraySettings.Load(File);

        settings.ShowMiniPanel.ShouldBeTrue();
        settings.MiniPanelPosition.ShouldBeNull();
    }

    [Fact]
    public void Settings_survive_a_restart()
    {
        new TraySettings { ShowMiniPanel = false, MiniPanelPosition = new Point(10, 20) }.Save(File);

        var loaded = TraySettings.Load(File);

        (loaded.ShowMiniPanel, loaded.MiniPanelPosition).ShouldBe((false, new Point(10, 20)));
    }

    [Fact]
    public void A_damaged_file_falls_back_to_the_defaults()
    {
        System.IO.File.WriteAllText(File, "{ not json");

        TraySettings.Load(File).ShowMiniPanel.ShouldBeTrue();
    }
}
