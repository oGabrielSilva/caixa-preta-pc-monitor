# PcMonitor.Brand

Shared Nuxt layer with the Caixa-Preta theme (ADR 0015, ADR 0021). Extended by `PcMonitor.Dashboard.Web` and `PcMonitor.Site` with `extends: ['../PcMonitor.Brand']`. It is the only copy of the theme: never copy these files into an app.

| Path | Responsibility |
|---|---|
| `nuxt.config.ts` | Settings shared by both apps: dark colour mode, fonts bundled at build (`@nuxt/fonts`), icons bundled (`icon.provider: 'none'`). A plain object: the layer has **no `package.json` and no `node_modules`**, so it lists no modules and imports nothing from npm. |
| `app/app.config.ts` | Nuxt UI colours (primary teal, neutral; semantic `success`/`warning`/`error` by meaning only) and the Material Symbols icon map. |
| `app/assets/css/brand.css` | Theme tokens: Google Sans Flex, radius 0, light/dark backgrounds. Each app imports `tailwindcss` and `@nuxt/ui` first, then this file by relative path. |
| `shared/locale-keys.ts` | Pure helpers for the locale tests of both apps (same keys in pt-BR and en, no dotted keys). |

Changing the theme here changes the dashboard **and** the landing page: build and look at both.
