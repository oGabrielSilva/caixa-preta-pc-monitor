# PcMonitor.Domain

Pure domain model: sensors, readings, sessions, incidents, tolerance policies and diagnosis concepts.

- **May depend on:** the .NET base class library only.
- **Must not depend on:** any other project or NuGet package (including `Microsoft.Extensions.*`).
- Types are immutable where possible (records, `init`). No I/O, no clocks, no static state.
- `Diagnosis/`: rules are independent `IDiagnosticRule` classes listed in `Rules/DiagnosticRules.All`; thresholds live only in `DiagnosisThresholds`/`RailTolerance`. Findings are codes + parameters, never display text. **Any change to a rule or threshold must bump `DiagnosisEngine.Version`** and update `docs/architecture/diagnosis.md`.
- Tests: `dotnet test --project tests/PcMonitor.Domain.Tests`
