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

    [Fact]
    public void Start_with_Windows_is_offered_once()
    {
        var first = TraySettings.Load(File);
        first.RunAtLoginOffered.ShouldBeFalse();

        first.RunAtLoginOffered = true;
        first.Save(File);

        TraySettings.Load(File).RunAtLoginOffered.ShouldBeTrue();
    }

    [Fact]
    public void Settings_saved_before_the_offer_existed_have_not_offered_it()
    {
        System.IO.File.WriteAllText(File, """{"ShowMiniPanel":false}""");

        TraySettings.Load(File).RunAtLoginOffered.ShouldBeFalse();
    }

    [Fact]
    public void The_panel_scale_defaults_to_one_and_survives_a_restart()
    {
        TraySettings.Load(File).MiniPanelScale.ShouldBe(1.0);

        new TraySettings { MiniPanelScale = 1.5 }.Save(File);

        TraySettings.Load(File).MiniPanelScale.ShouldBe(1.5);
    }
}
