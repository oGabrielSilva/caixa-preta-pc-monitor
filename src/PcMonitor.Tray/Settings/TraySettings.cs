using System.IO;
using System.Text.Json;
using System.Windows;

namespace PcMonitor.Tray.Settings;

/// <summary>The user's tray app settings, kept as JSON in <c>%LocalAppData%\CaixaPreta</c>.</summary>
internal sealed class TraySettings
{
    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaixaPreta", "settings.json");

    public bool ShowMiniPanel { get; set; } = true;

    /// <summary>Top-left corner of the panel; <c>null</c> until the user moves it.</summary>
    public Point? MiniPanelPosition { get; set; }

    /// <summary>Size of the panel relative to its natural size (also the page zoom), chosen by resizing it.</summary>
    public double MiniPanelScale { get; set; } = 1.0;

    /// <summary>
    /// Whether "start with Windows" was already turned on for this user. The app turns it on at its first run, once,
    /// so the user's later choice in the tray menu is never overridden.
    /// </summary>
    public bool RunAtLoginOffered { get; set; }

    /// <summary>The saved settings, or the defaults when the file is missing or damaged.</summary>
    public static TraySettings Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<TraySettings>(File.ReadAllText(path)) ?? new TraySettings() : new TraySettings();
        }
        catch (JsonException)
        {
            return new TraySettings();
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}
