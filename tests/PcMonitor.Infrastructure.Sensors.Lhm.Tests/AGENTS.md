# PcMonitor.Infrastructure.Sensors.Lhm.Tests

Tests for `PcMonitor.Infrastructure.Sensors.Lhm`: board profiles and sensor selection, using NSubstitute fakes of LibreHardwareMonitor's `ISensor`/`IHardware`. `LhmSensorSource` itself touches real hardware and is verified on the machine, not here.

- xUnit v3 + Shouldly. Test names read as sentences: `Subject_behaviour_condition`.
- Write the failing test first; assert behaviour, not implementation details.
- Run: `dotnet test --project tests/PcMonitor.Infrastructure.Sensors.Lhm.Tests`
