# PcMonitor.Dashboard

Host for the unprivileged dashboard: ASP.NET Core minimal API under `/api`, OpenAPI document and the generated SPA in `wwwroot`. Listens on `127.0.0.1` only (port from `Dashboard:Port`, default 5280).

- **May depend on:** Application, `PcMonitor.Infrastructure.Persistence.Sqlite` (read side only).
- **Must not depend on:** sensor or Windows adapters; never writes to the database.
- One `Api/<Resource>/<Resource>Endpoints.cs` per resource, exposing `Map<Resource>Endpoints(this RouteGroupBuilder)`; DTOs are records next to it.
- Tests: `dotnet test --project tests/PcMonitor.Dashboard.Tests`
