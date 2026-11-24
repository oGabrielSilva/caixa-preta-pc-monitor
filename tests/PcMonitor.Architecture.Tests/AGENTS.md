# PcMonitor.Architecture.Tests

Enforces the dependency rules between layers with ArchUnitNET (plus a plain reflection check for Domain).

- Layer names and type sets live in `PcMonitorArchitecture.cs`; add new rules to `LayerDependencyTests.cs` using those helpers.
- Rules use `WithoutRequiringPositiveResults()` while layers are still empty; keep it so rules do not fail when a layer has no types yet.
- When adding a production project, add it to `PcMonitorArchitecture` and give it rules.
- Run: `dotnet test --project tests/PcMonitor.Architecture.Tests`
