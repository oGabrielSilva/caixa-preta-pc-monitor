# PcMonitor.Infrastructure.Sensors.Lhm

Adapter that implements the sensor-reading port with LibreHardwareMonitorLib (PawnIO driver, requires administrator).

- **May depend on:** `PcMonitor.Application`, `LibreHardwareMonitorLib`.
- **Must not depend on:** other Infrastructure projects or hosts.
- This is the only project allowed to reference LibreHardwareMonitor types; nothing from the library leaks through its public API.
- Board support lives in `Profiles/<board>.json` (embedded): `exclude` regexes, and per-sensor `name`, `role` and `multiplier` (voltage dividers). Add a board by adding a file; no code change.
- Driver-backed sensors are recorded only when PawnIO is installed and the process is elevated (`SourceStatus.Resolve`).
