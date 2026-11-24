# PcMonitor.Application

Use cases and the ports (interfaces) they need. Orchestrates the domain; knows nothing about LibreHardwareMonitor, SQLite or Windows APIs.

- **May depend on:** `PcMonitor.Domain`; `TimeProvider` for time.
- **Must not depend on:** Infrastructure projects, hosts, concrete I/O.
- One use case per class. Read and write ports are separate interfaces.
- Tests: `dotnet test --project tests/PcMonitor.Application.Tests` (hand-written fakes for ports).
