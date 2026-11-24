# PcMonitor.Collector

Host for the privileged collector Windows service (`PcMonitor.Collector`, LocalSystem). Composition root only: DI registration, options, logging, service lifetime.

- **May depend on:** Application and all Infrastructure projects.
- **Must not contain:** business rules. If logic appears here, move it to Application.
- Run locally (console): `dotnet run --project src/PcMonitor.Collector` from an elevated terminal.
