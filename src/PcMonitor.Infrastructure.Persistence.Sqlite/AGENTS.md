# PcMonitor.Infrastructure.Persistence.Sqlite

Adapter that implements the storage ports with SQLite (`Microsoft.Data.Sqlite` + Dapper): WAL, `synchronous=FULL`, embedded SQL migrations.

- **May depend on:** `PcMonitor.Application`, `Microsoft.Data.Sqlite`, `Dapper`.
- **Must not depend on:** other Infrastructure projects or hosts.
- SQL is explicit and lives next to the repository that runs it. The frame BLOB format is owned by a single codec class.
- Tests: `dotnet test --project tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests` (real SQLite files in a temp directory).
