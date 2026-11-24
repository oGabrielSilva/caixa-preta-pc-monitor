# PcMonitor.Dashboard.Web

Frontend of the dashboard: Nuxt 4 in SPA mode (`ssr: false`), Nuxt UI v4, TypeScript strict, i18n (`pt-BR` default, `en`). Built with `nuxt generate` into static files that `PcMonitor.Dashboard` serves; there is no Node runtime on the user's machine.

## Structure (`app/`)

| Folder | Responsibility |
|---|---|
| `pages/` | Route composition only. No logic, no API calls. |
| `components/<area>/` | UI by area: `live/`, `incidents/`, `history/`, `shared/`. Components never call the API directly. |
| `composables/` | State and API access (`use*`). The only place that talks to `api/`. |
| `api/` | `client.ts` (the single HTTP client) and the **generated** `schema.d.ts`. Never edit `schema.d.ts` by hand. |
| `utils/` | Pure functions and static data (e.g. `navigation.ts`). |
| `app.config.ts` | Semantic colours: `success` = in range, `warning` = attention, `error` = out of range. Use colours by meaning only. |

User-facing text always comes from `i18n/locales/*.json`; both files must have the same keys (a test enforces it).

## Commands (Node 26 LTS, pnpm)

```powershell
pnpm install
pnpm dev          # needs the dashboard API on 127.0.0.1:5280
pnpm lint
pnpm typecheck
pnpm test
pnpm generate     # static build into .output/public
pnpm gen:api      # regenerate app/api/schema.d.ts from the OpenAPI document
```

Tests are pure Node by default; a test that needs the Nuxt runtime starts with `// @vitest-environment nuxt`.

Supply-chain policy: pnpm refuses packages published less than a day ago, and only the packages listed in `pnpm-workspace.yaml` may run install scripts. Keep both.
