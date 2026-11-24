# PcMonitor.Infrastructure.Sensors.Lhm

Adapter that implements the sensor-reading port with LibreHardwareMonitorLib (PawnIO driver, requires administrator).

- **May depend on:** `PcMonitor.Application`, `LibreHardwareMonitorLib`.
- **Must not depend on:** other Infrastructure projects or hosts.
- This is the only project allowed to reference LibreHardwareMonitor types; nothing from the library leaks through its public API.
