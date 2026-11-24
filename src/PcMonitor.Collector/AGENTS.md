# PcMonitor.Collector

Host for the privileged collector Windows service (`PcMonitor.Collector`, LocalSystem). Composition root only: DI registration, options, logging, service lifetime.

- **May depend on:** Application and all Infrastructure projects.
- **Must not contain:** business rules. If logic appears here, move it to Application.
- Layout: `Configuration/` (options), `Hosting/` (composition root `AddCollector`, service lifetime, signals, start-up incident detection), `Sampling/` (sampler thread, watchdog), `Retention/` (periodic job).
- Start-up order in `Program.cs`: migrate the schema → `StartupAnalysis` (detect incidents, then diagnose; each step isolated, never fatal) → run the host (sampling starts a new session).
- Run locally (console): `dotnet run --project src/PcMonitor.Collector` from an elevated terminal (without elevation the session is `DriverMissing` and only load sensors are recorded). Set `Collector__DatabasePath` to keep test runs out of `%ProgramData%`.
- Install as a service: `dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector` (normal user), then `pwsh scripts/Install-Collector.ps1` (administrator).
