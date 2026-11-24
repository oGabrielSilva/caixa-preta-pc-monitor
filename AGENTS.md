# AGENTS.md — PC Monitor

Windows hardware monitor that records sensors in a power-loss-safe way, detects unexpected shutdowns and explains their likely cause. Two processes: a privileged **Collector** (Windows service) writes to SQLite; an unprivileged **Dashboard** reads it and serves a Nuxt UI SPA on `127.0.0.1:5280`.

Read first: [architecture study](docs/estudos/2026-11-24-estudo-arquitetura.md) · [architecture docs](docs/architecture/README.md) · [ADRs](docs/adr/README.md).

## Map

| Path | Responsibility |
|---|---|
| `src/PcMonitor.Domain` | Pure model and rules. No dependencies. |
| `src/PcMonitor.Application` | Ports (interfaces) and use cases. |
| `src/PcMonitor.Infrastructure.*` | One adapter per project, implementing Application ports. |
| `src/PcMonitor.Collector` | Host: Windows service, composition only. |
| `src/PcMonitor.Dashboard` | Host: ASP.NET Core API + static SPA, composition only. |
| `src/PcMonitor.Dashboard.Web` | Frontend: Nuxt 4 + Nuxt UI. |
| `tests/*` | One test project per production project, plus architecture tests. |
| `spikes/` | Throwaway experiments. Not part of the solution; no quality rules. |
| `docs/` | Study, plans, architecture docs, ADRs (Portuguese). |

## Commands

```powershell
dotnet build PcMonitor.slnx
dotnet test --solution PcMonitor.slnx
dotnet format PcMonitor.slnx --verify-no-changes
```

Frontend commands: see `src/PcMonitor.Dashboard.Web/AGENTS.md`. CI (`.github/workflows/ci.yml`) runs all of the above plus the API contract check, `scripts/Test-AgentDocs.ps1` and jscpd (`.jscpd.json`, threshold 1%).

## Rules

- `net10.0` (.NET 10 LTS). Warnings are errors; fix them, never suppress them without a comment explaining why.
- Package versions live only in `Directory.Packages.props`. Microsoft platform packages stay on the `10.0.x` line.
- Dependencies point inward: hosts → infrastructure → application → domain. `tests/PcMonitor.Architecture.Tests` enforces it.
- No duplicated code. Extract a shared type instead of copying; jscpd runs in CI.
- Code, identifiers, commits and log messages in English. Documentation in Portuguese.
- DTOs are defined once in C#. TypeScript API types are generated, never hand-written.
- Every folder with an `AGENTS.md` has a `CLAUDE.md` containing exactly `@AGENTS.md`. Put instructions in `AGENTS.md` only. Check: `pwsh scripts/Test-AgentDocs.ps1`.
- Architecture changes update `docs/architecture/` (diagrams live only there); decisions get a new ADR in `docs/adr/`.
- Tests: xUnit v3 on Microsoft.Testing.Platform, Shouldly assertions. Write the failing test first.
