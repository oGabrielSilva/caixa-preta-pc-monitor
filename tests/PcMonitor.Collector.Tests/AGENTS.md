# PcMonitor.Collector.Tests

Tests for `PcMonitor.Collector`: the stall watchdog, Windows service signals and option validation. The sampler thread and the service lifetime are verified by running the collector on the machine.

- xUnit v3 + Shouldly. Test names read as sentences: `Subject_behaviour_condition`.
- Write the failing test first; assert behaviour, not implementation details.
- Run: `dotnet test --project tests/PcMonitor.Collector.Tests`
