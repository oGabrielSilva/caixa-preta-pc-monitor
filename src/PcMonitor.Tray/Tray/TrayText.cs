using System.Globalization;

namespace PcMonitor.Tray.Tray;

/// <summary>Texts of the tray icon and its menu, in Portuguese (default) or English.</summary>
internal sealed record TrayText(string Tooltip, string Open, string ShowMiniPanel, string RunAtLogin, string Exit)
{
    public static readonly TrayText Portuguese = new("Caixa-Preta", "Abrir", "Mostrar painel fixo", "Iniciar com o Windows", "Sair");

    public static readonly TrayText English = new("Caixa-Preta", "Open", "Show the always-on-top panel", "Start with Windows", "Exit");

    public static TrayText ForCurrentCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? English : Portuguese;
}
