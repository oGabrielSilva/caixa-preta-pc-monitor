# PcMonitor.Infrastructure.Windows.Tests

Tests for `PcMonitor.Infrastructure.Windows`: event XML parsing, the 6008 binary time and the mapping to `UnexpectedShutdown`, using real event XML captured on a test machine (`Fixtures/`, computer name anonymized as `DESKTOP-TEST`). One integration test reads this machine's System log (read-only).

- Never commit fixtures with real computer or user names.
- xUnit v3 + Shouldly. Test names read as sentences: `Subject_behaviour_condition`.
- Run: `dotnet test --project tests/PcMonitor.Infrastructure.Windows.Tests`
