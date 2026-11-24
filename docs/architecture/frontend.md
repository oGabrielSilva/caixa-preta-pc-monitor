# Frontend

O painel é uma SPA Nuxt 4 + Nuxt UI em `src/PcMonitor.Dashboard.Web`, gerada como arquivos estáticos e servida pelo `PcMonitor.Dashboard` ([ADR 0007](../adr/0007-nuxt-spa-static-with-nuxt-ui.md)). Convenções de pastas: [AGENTS.md do frontend](../../src/PcMonitor.Dashboard.Web/AGENTS.md).

## Contrato da API ✅

Os DTOs existem só em C#. O TypeScript é gerado ([ADR 0008](../adr/0008-openapi-generated-typescript-contract.md)).

```mermaid
flowchart LR
    CS[Records C#<br/>Api/*/*Response.cs] -->|dotnet build| OA[openapi/PcMonitor.Dashboard.json]
    OA -->|pnpm gen:api| TS[app/api/schema.d.ts]
    TS --> CL[app/api/client.ts<br/>openapi-fetch]
    CL --> CO[composables/use*]
    CO --> CP[components / pages]
    OA -. CI: git diff --exit-code .-> CHK{{Contrato atualizado?}}
    TS -.-> CHK
```

## Publicação ✅

```mermaid
flowchart LR
    P[dotnet publish<br/>PcMonitor.Dashboard] --> T[BuildDashboardWeb<br/>build/DashboardWeb.targets]
    T --> I[pnpm install --frozen-lockfile]
    I --> G[pnpm generate]
    G --> W[.output/public → wwwroot]
    W --> E[PcMonitor.Dashboard.exe<br/>127.0.0.1:5280]
```

`dotnet build` e `dotnet test` não precisam de Node. Rotas desconhecidas em `/api` respondem 404; qualquer outra rota serve `index.html`.
