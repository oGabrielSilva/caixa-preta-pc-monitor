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
| `app.config.ts` | Theme (ADR 0015): primary teal, neutral, Material Symbols icons. Semantic colours: `success` = in range, `warning` = attention, `error` = out of range. Use colours by meaning only. |
| `assets/css/main.css` | Theme tokens: fonts (bundled at build by `@nuxt/fonts`), radius 0, light/dark backgrounds. |

User-facing text always comes from `i18n/locales/*.json`; both files must have the same keys and no key may contain a dot (tests enforce both).

- Charts only through `components/shared/TimeSeriesChart.vue` (unique labels, breaks where nothing was recorded). Series colours are categories, not statuses.
- Diagnosis texts: `utils/finding-keys.ts` maps every generated `FindingCode` to an i18n key; parameters are formatted by `utils/findings.ts`.
- Ranges and limits come from the API (`SensorResponse`); never hard-code a threshold.
- Icons: Material Symbols only (`i-material-symbols-*`, bundled; a test checks every name exists).
- Loaders return `ApiResult` (`composables/api-result.ts`); pages show `shared/StorageNotice` for a problem.
- Component tests mount with `mountPtBR` (`test/support/nuxt.ts`): the test runtime would otherwise pick the browser language.

## Commands (Node 26 LTS, pnpm)

```powershell
pnpm install
pnpm dev          # needs the dashboard API on 127.0.0.1:5280
pnpm lint
pnpm typecheck    # app (nuxt typecheck) + tests and nuxt.config (test/tsconfig.json)
pnpm test
pnpm generate     # static build into .output/public
pnpm gen:api      # regenerate app/api/schema.d.ts from the OpenAPI document
pnpm icons        # regenerate the icons from assets/brand/caixa-preta.svg (ADR 0017)
```

Tests are pure Node by default; a test that needs the Nuxt runtime starts with `// @vitest-environment nuxt`.

Supply-chain policy: pnpm refuses packages published less than a day ago, and only the packages listed in `pnpm-workspace.yaml` may run install scripts. Keep both.
