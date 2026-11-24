# PcMonitor.Dashboard.Tests

Tests for `PcMonitor.Dashboard`: endpoints and SPA hosting through `WebApplicationFactory<Program>`, and option validation.

- xUnit v3 + Shouldly. Test names read as sentences: `Subject_behaviour_condition`.
- Write the failing test first; assert behaviour, not implementation details.
- `Support/DashboardFactory` runs the real host over a test database (or none); `Support/SeededDashboard` adds a `DatabaseSeed` and a frozen clock (`realTime: true` for streams).
- Run: `dotnet test --project tests/PcMonitor.Dashboard.Tests`
