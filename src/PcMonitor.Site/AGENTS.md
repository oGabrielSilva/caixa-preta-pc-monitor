# PcMonitor.Site

Public landing page of Caixa-Preta (ADR 0021): Nuxt 4 + Nuxt UI v4, rendered to static HTML by `nuxt generate` (SSR at build time, no server) and published on GitHub Pages at `https://ogabrielsilva.github.io/caixa-preta-pc-monitor/` by `.github/workflows/pages.yml`. It extends the shared theme layer `../PcMonitor.Brand`. Portuguese (`pt-BR`) at `/`, English under `/en/`.

## Structure

| Path | Responsibility |
|---|---|
| `app/pages/` | Route composition only: `index.vue` (home) and `releases.vue` (`/versoes`, `/en/releases`). |
| `app/components/<area>/` | UI by area: `home/`, `releases/`, `shared/`. |
| `app/composables/` | `usePageSeo` (title, description, Open Graph, canonical, hreflang). |
| `shared/utils/` | Pure functions shared by the app, the server routes and the build module: `site.ts` (URLs, repository, author), `releases.ts` (GitHub Releases mapping), `seo.ts` (absolute URLs, JSON-LD, sitemap, robots). |
| `modules/github-releases.ts` | Reads the GitHub Releases once at build time and exposes them as `#releases`. A failed request (private repository, no network, rate limit) never fails the build: the site shows "first release coming soon". |
| `server/routes/` | `sitemap.xml`, `robots.txt`, `llms.txt`, prerendered into static files. |
| `public/` | Icons (generated from `assets/brand/`), screenshots (`screens/`), Open Graph images (`og/`). |
| `screenshots/` | How the screenshots are taken, and the demo database for the incident screen. |

## Rules

- User-facing text always comes from `i18n/locales/*.json`. Both files have the same keys, no key contains a dot, and no text contains an e-mail address (tests enforce all three).
- Absolute URLs only through `absoluteUrl()` (`shared/utils/seo.ts`): the project path `/caixa-preta-pc-monitor/` must appear exactly once.
- The visible FAQ and the `FAQPage` JSON-LD come from the same list (`app/utils/faq.ts`).
- Nothing is loaded from a third party at runtime: no analytics, no font or icon CDN (the Brand layer bundles both).
- Honest copy: the diagnosis is a "likely cause", never a certainty; the installer is not code-signed and the FAQ says so. The incident screenshot comes from a demo database and is labelled "sample data".
- The author appears only as "Gabriel Silva" with the GitHub profile; never an e-mail address.
- Never create a folder named `releases/` (the root `.gitignore` ignores it).

## Commands (Node 26 LTS, pnpm)

```powershell
pnpm install
pnpm dev          # http://localhost:3000/caixa-preta-pc-monitor/
pnpm lint
pnpm typecheck
pnpm test
pnpm generate     # static site into .output/public (reads the GitHub Releases; set GITHUB_TOKEN to avoid the rate limit)
pnpm og           # regenerate the Open Graph images
pnpm screens      # optimise the screenshots in screenshots/raw into public/screens
```

Supply-chain policy: pnpm refuses packages published less than a day ago, and only the packages listed in `pnpm-workspace.yaml` may run install scripts. Keep both.
