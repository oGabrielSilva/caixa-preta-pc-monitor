# AGENTS.md — Caixa-Preta (PC Monitor)

Windows hardware monitor that records sensors in a power-loss-safe way, detects unexpected shutdowns and explains their likely cause. Product name **Caixa-Preta** (ADR 0017); code keeps `PcMonitor.*`. Three processes: a privileged **Collector** (Windows service) writes to SQLite; an unprivileged **Dashboard** reads it and serves a Nuxt UI SPA on `127.0.0.1:5280`; the **tray app** (`CaixaPreta.exe`, user session) shows that SPA in WebView2 windows, including the always-on-top mini panel. One Inno Setup installer ships all three.

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
| `src/PcMonitor.Tray` | Tray app (WPF + WebView2): main window, mini panel, tray icon. Talks HTTP only. |
| `installer/` | Inno Setup script for `CaixaPreta-Setup-<version>.exe`. |
| `scripts/` | Install/uninstall scripts (Windows PowerShell 5.1), `Build-Release.ps1`, repo checks. |
| `assets/brand/` | The icon: one SVG source, generated ICO/PNG. |
| `tests/*` | One test project per production project, plus architecture tests. |
| `spikes/` | Throwaway experiments. Not part of the solution; no quality rules. |
| `docs/` | Study, plans, architecture docs, ADRs (Portuguese). |

## Commands

```powershell
dotnet build PcMonitor.slnx
dotnet test --solution PcMonitor.slnx
dotnet format PcMonitor.slnx --verify-no-changes
powershell -NoProfile -File scripts/Test-Scripts.ps1   # install scripts must run on Windows PowerShell 5.1
powershell -File scripts/Build-Release.ps1             # installer (needs Inno Setup 6), not elevated
```

Frontend commands: see `src/PcMonitor.Dashboard.Web/AGENTS.md`. CI (`.github/workflows/ci.yml`) runs the checks above (the installer is built by `release.yml` on a `v*` tag) plus the API contract check, `scripts/Test-AgentDocs.ps1` and jscpd (`.jscpd.json`, threshold 0%: any clone fails; `using` directives are ignored).

## Rules

- .NET 10 LTS: `net10.0`, or `net10.0-windows` for the Windows-only collector side (Collector, Sensors.Lhm, Infrastructure.Windows, Tray and their tests). Warnings are errors; fix them, never suppress them without a comment explaining why.
- Package versions live only in `Directory.Packages.props`. Microsoft platform packages stay on the `10.0.x` line.
- Dependencies point inward: hosts → infrastructure → application → domain. `tests/PcMonitor.Architecture.Tests` enforces it.
- No duplicated code. Extract a shared type instead of copying; jscpd runs in CI.
- Code, identifiers, commits and log messages in English. Documentation in Portuguese.
- DTOs are defined once in C#. TypeScript API types are generated, never hand-written.
- Every folder with an `AGENTS.md` has a `CLAUDE.md` containing exactly `@AGENTS.md`. Put instructions in `AGENTS.md` only. Check: `pwsh scripts/Test-AgentDocs.ps1`.
- Architecture changes update `docs/architecture/` (diagrams live only there); decisions get a new ADR in `docs/adr/`.
- Tests: xUnit v3 on Microsoft.Testing.Platform, Shouldly assertions. Write the failing test first.
