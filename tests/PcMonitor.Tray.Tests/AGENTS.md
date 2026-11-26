# PcMonitor.Tray.Tests

Tests for `PcMonitor.Tray` (the Caixa-Preta tray app): window placement, settings, single instance, run at login,
the dashboard address and the app's dependencies. The windows, the tray icon and WebView2 are verified by running the app.

- xUnit v3 + Shouldly. Test names read as sentences: `Subject_behaviour_condition`.
- Write the failing test first; assert behaviour, not implementation details.
- Run: `dotnet test --project tests/PcMonitor.Tray.Tests`
