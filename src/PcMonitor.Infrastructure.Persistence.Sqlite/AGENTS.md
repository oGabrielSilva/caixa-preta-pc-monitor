# PcMonitor.Infrastructure.Persistence.Sqlite

Adapter that implements the storage ports with SQLite (`Microsoft.Data.Sqlite` + Dapper): WAL, `synchronous=FULL`, embedded SQL migrations.

- **May depend on:** `PcMonitor.Application`, `Microsoft.Data.Sqlite`, `Dapper`.
- **Must not depend on:** other Infrastructure projects or hosts.
- SQL is explicit and lives next to the repository that runs it (`*Sql.cs`); never duplicate a statement, reuse the constant. The frame BLOB format is owned by a single codec class.
- Folders: `Monitoring/` (sessions, segments, frames), `Frames/` (codec, reader), `Incidents/` (incident reader/writer: an incident and the closure of its session are one transaction), `Diagnosis/` (diagnosis store and `IncidentEvidenceAssembler`: telemetry window by role, related events owned by each incident), `Live/`, `History/`, `Storage/` (read-only readers for the dashboard), `Retention/`, `Migrations/` (`NNN_name.sql`, never edit an applied one).
- Tests: `dotnet test --project tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests` (real SQLite files in a temp directory).
