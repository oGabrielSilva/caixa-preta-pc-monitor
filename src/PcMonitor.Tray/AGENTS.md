# PcMonitor.Tray

The Caixa-Preta tray app (`CaixaPreta.exe`, WPF, user session, starts with Windows). It hosts the dashboard in two
WebView2 windows (ADR 0018): the main window and the always-on-top panel (`/mini`), plus the tray icon.

- **Depends on no PC Monitor project:** it only talks HTTP to the dashboard (`127.0.0.1`, port from the installed
  dashboard's `appsettings.json`). Every rule (ranges, colours, live state) lives in the dashboard; never reimplement one here.
- Layout: `Windows/` (main and mini windows, `WebViewHost` shared by both, placement), `Tray/` (icon, menu, texts),
  `Startup/` (single instance, run at login, dashboard address), `Settings/` (user settings in `%LocalAppData%\CaixaPreta`).
- Messages from the pages (`chrome.webview.postMessage`): `{ type: "open", path }` shows the main window on that page;
  `{ type: "drag" }` starts moving the mini panel.
- WPF and Windows Forms share type names: use aliases, never the implicit Windows Forms usings.
- Tests: `dotnet test --project tests/PcMonitor.Tray.Tests` (logic only; windows and WebView2 are checked by running the app).
