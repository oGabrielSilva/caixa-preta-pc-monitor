# PcMonitor.Dashboard

Host for the unprivileged dashboard: ASP.NET Core minimal API under `/api`, OpenAPI document and the generated SPA in `wwwroot`. Listens on `127.0.0.1` only (port from `Dashboard:Port`, default 5280).

- **May depend on:** Application, `PcMonitor.Infrastructure.Persistence.Sqlite` (read side only).
- **Must not depend on:** sensor or Windows adapters; never writes to the database.
- One `Api/<Resource>/<Resource>Endpoints.cs` per resource, exposing `Map<Resource>Endpoints(this RouteGroupBuilder)`; DTOs are records next to it, each with a static factory from the Application type.
- Data groups call `RequireStorage()` (503 ProblemDetails `storage.*` until the database can be read). `/api/health` never does.
- Ranges and limits shown to the user come from `Api/Sensors/SensorPresentation` (the diagnosis constants). Readings without a value are `null` in JSON.
- Never expose event or incident `data_json`: it can name the computer and the user.
- `/api/live/stream` is server-sent events of one type (`LiveEvent`), fed by `LiveFeed`.
- Runs as the Windows service `PcMonitor.Dashboard` (account `NT SERVICE\PcMonitor.Dashboard`); logs to the Event Log only as a service.
- Tests: `dotnet test --project tests/PcMonitor.Dashboard.Tests`
