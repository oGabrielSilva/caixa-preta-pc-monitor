# PcMonitor.Infrastructure.Windows

Adapter for Windows-specific sources: Event Log (Kernel-Power 41, 6008, 1074, BugCheck 1001, WHEA) and system information.

- **May depend on:** `PcMonitor.Application`, Windows-only .NET APIs.
- **Must not depend on:** other Infrastructure projects or hosts.
- `EventLog/`: `WindowsEventLogReader` (XPath queries in `EventLogQueries`) → `EventXml.Parse` (pure) → `UnexpectedShutdownMapper`. Event 6008's text is localized; its time comes from the binary `SYSTEMTIME` (`LastAliveTime`).
- Reading the System log needs no elevation. Tests: `dotnet test --project tests/PcMonitor.Infrastructure.Windows.Tests` (fixtures are real events with the computer name anonymized).
