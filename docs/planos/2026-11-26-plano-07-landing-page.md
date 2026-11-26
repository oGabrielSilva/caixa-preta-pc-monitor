# Plano 07 — Landing page do Caixa-Preta e post do LinkedIn

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** uma landing page estática (SSG), bilíngue (pt-BR em `/`, en em `/en/`), em Nuxt UI com o tema do painel, com histórico de versões vindo das Releases do GitHub, SEO/AEO/GEO completos, publicada pelo GitHub Pages do próprio repositório, mais um rascunho de post para o LinkedIn.

**Architecture:** o tema sai do `Dashboard.Web` para uma camada Nuxt sem dependências próprias (`src/PcMonitor.Brand`), estendida pelo painel e pelo novo app `src/PcMonitor.Site`. O site é gerado com `nuxt generate` (SSR no build). Um módulo local busca as releases uma vez no build e as expõe como import virtual. Funções puras em `shared/utils/` montam URLs, JSON-LD, sitemap, robots e `llms.txt`, e todas têm testes. Um workflow `pages.yml` publica no Pages, e o `release.yml` o dispara depois de cada release.

**Tech Stack:** Nuxt 4.6.4 · Nuxt UI 4.13.1 · `@nuxtjs/i18n` 10.6.0 · Tailwind 4 · Vitest 5 · `@resvg/resvg-js` (imagem OG) · `sharp` (otimizar capturas, só em script de dev) · GitHub Actions (`actions/upload-pages-artifact`, `actions/deploy-pages`) · arquivo C# executável com `dotnet run` para o banco de demonstração.

**Spec:** [docs/superpowers/specs/2026-11-26-landing-page-design.md](../superpowers/specs/2026-11-26-landing-page-design.md)

## Global Constraints

- URL base: `https://ogabrielsilva.github.io/caixa-preta-pc-monitor/`. `app.baseURL = '/caixa-preta-pc-monitor/'`. Repositório: `https://github.com/oGabrielSilva/caixa-preta-pc-monitor`.
- Mesmas versões de pacotes do `Dashboard.Web` (`nuxt 4.6.4`, `@nuxt/ui 4.13.1`, `@nuxtjs/i18n 10.6.0`, `tailwindcss 4.3.5`, `vue 3.5.46`, `typescript 6.0.3`, `vitest 5.1.1`, `@nuxt/eslint 1.17.0`, `eslint 10.15.0`), o mesmo `packageManager` (`pnpm@12.23.0`) e `engines.node >=26`.
- Nenhum rastreador, analytics, CDN de fontes ou de ícones em tempo de execução: as fontes vêm do `@nuxt/fonts` no build e os ícones com `icon.provider: 'none'`, `fallbackToApi: false`.
- O e-mail do dono nunca aparece em lugar nenhum: texto, metadados, JSON-LD, imagens, commits do site. O autor é só "Gabriel Silva" + `https://github.com/oGabrielSilva`.
- Capturas sem nome da máquina, usuário do Windows ou caminhos pessoais. A tela de incidente usa o banco de demonstração, com a legenda "dados de exemplo" / "sample data".
- Tom honesto: "causa provável", nunca certeza. O instalador não é assinado e o FAQ explica o alerta do SmartScreen.
- jscpd 0%: nada copiado entre `Dashboard.Web` e `Site`. O que é comum vai para `src/PcMonitor.Brand`.
- Nenhuma pasta chamada `releases/` (o `.gitignore` ignora `[Rr]eleases/`). A página de versões é o arquivo `app/pages/releases.vue`.
- Código em inglês, documentação e textos pt-BR em português. Cada pasta nova com `AGENTS.md` + `CLAUDE.md` (`@AGENTS.md`).
- **Não publicar:** sem `git push`, sem criar o repositório remoto, sem postar. Commits locais por tarefa são permitidos.
- Visual: invocar a skill `frontend-design:frontend-design` antes da Task 6. Nada de "hero + 3 cards". A identidade é a metáfora da caixa-preta de voo, com cantos retos (`--ui-radius: 0`) e teal/neutral.

## Review Focus

1. **Repositório ainda privado ou sem releases** (o estado de hoje): o `nuxt generate` precisa passar offline, com 404 ou com limite de requisições. O botão de download cai para a página de releases e `/versoes` mostra o estado vazio. Teste em `releases.spec.ts` (`fetchReleases` com 404 e com erro de rede → `[]`).
2. **Base path `/caixa-preta-pc-monitor/`**: canonical, `hreflang`, `og:url`, `og:image`, sitemap e links internos precisam ser absolutos e incluir o subcaminho uma única vez. Testes em `seo.spec.ts` (`absoluteUrl('/')`, `absoluteUrl('/en/releases')`) e uma verificação no HTML gerado (Task 9).
3. **A release mais recente é uma pré-release, ou não tem `.exe`**: o download aponta para a estável mais recente que tenha instalador. Teste em `releases.spec.ts`.
4. **O FAQ visível e o JSON-LD `FAQPage` divergem**: os dois saem da mesma lista de chaves i18n. Teste em `seo.spec.ts`, mais a checagem do HTML gerado.
5. **Notas de release com HTML/markdown arbitrário**: o markdown da release é renderizado como texto seguro, sem `v-html` cru. Teste em `releases.spec.ts` (`<script>` nas notas não vira elemento).

---

### Task 1: Camada de tema compartilhada `src/PcMonitor.Brand`

**Files:**
- Create: `src/PcMonitor.Brand/nuxt.config.ts`, `src/PcMonitor.Brand/app/app.config.ts` (movido do painel), `src/PcMonitor.Brand/app/assets/css/brand.css`, `src/PcMonitor.Brand/shared/locale-keys.ts`, `src/PcMonitor.Brand/AGENTS.md`, `src/PcMonitor.Brand/CLAUDE.md`
- Modify: `src/PcMonitor.Dashboard.Web/nuxt.config.ts` (`extends`, remove `fonts`/`icon`/`colorMode` que passam para a camada), `src/PcMonitor.Dashboard.Web/app/assets/css/main.css` (fica só `@import`s + `.mini-window`), `src/PcMonitor.Dashboard.Web/test/support/locale-keys.ts` (reexporta da camada), `src/PcMonitor.Dashboard.Web/AGENTS.md`
- Delete: `src/PcMonitor.Dashboard.Web/app/app.config.ts`
- Test: `src/PcMonitor.Dashboard.Web/test/unit/nuxt-config.spec.ts`

**Interfaces:**
- Produces: a camada `../PcMonitor.Brand` (o nome da camada em `$meta.name` é `brand`). O `brand.css` contém `@theme`, a regra dos `h1..h6`, `--ui-radius: 0` e as variáveis claro/escuro de hoje. Ele **não** importa `tailwindcss` nem `@nuxt/ui`: cada app faz `@import "tailwindcss"; @import "@nuxt/ui"; @import "<caminho relativo>/PcMonitor.Brand/app/assets/css/brand.css";`. A camada `nuxt.config.ts` define só `fonts`, `icon` e `colorMode` (sem `modules`, porque a camada não tem `node_modules`). `shared/locale-keys.ts` exporta `localeKeys(messages: object): string[]` e `dottedKeys(messages: object): string[]`.

- [ ] **Step 1: Testes que falham** em `nuxt-config.spec.ts`: `expect(config.extends).toEqual(['../PcMonitor.Brand'])`. Num novo `brand-layer.spec.ts`, o `app.config` da camada tem `ui.colors.primary === 'teal'` e `ui.colors.neutral === 'neutral'`, e `src/PcMonitor.Dashboard.Web/app/app.config.ts` não existe mais.
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL nos dois testes.
- [ ] **Step 3:** mover os arquivos conforme acima. O teste que já existe (`icon.provider: 'none'`) passa a ler o config mesclado: ajuste-o para importar o `nuxt.config` da camada.
- [ ] **Step 4:** no `Dashboard.Web`, rodar `pnpm lint && pnpm typecheck && pnpm test && pnpm generate`. Esperado: tudo verde. Abrir o painel gerado (servido pelo dashboard em `127.0.0.1:5280` depois de publicar, ou `pnpm dev`) e conferir no navegador embutido que as cores, as fontes e os cantos continuam iguais.
- [ ] **Step 5:** `powershell -File scripts/Build-Release.ps1` para confirmar que o instalador ainda é gerado (o build do SPA precisa enxergar a camada).
- [ ] **Step 6: Commit** `refactor: extract the shared theme into a Nuxt layer`

### Task 2: Esqueleto do app `src/PcMonitor.Site`

**Files:**
- Create: `src/PcMonitor.Site/{package.json,pnpm-workspace.yaml,nuxt.config.ts,tsconfig.json,eslint.config.mjs,vitest.config.ts,AGENTS.md,CLAUDE.md}`, `app/app.vue`, `app/assets/css/main.css`, `i18n/site-locales.ts`, `i18n/locales/{pt-BR,en}.json`, `shared/utils/site.ts`, `test/unit/locales.spec.ts`, `test/unit/nuxt-config.spec.ts`
- Modify: `AGENTS.md` (raiz: linhas do mapa para `PcMonitor.Brand` e `PcMonitor.Site`)

**Interfaces:**
- Produces: `shared/utils/site.ts` com `SITE_ORIGIN = 'https://ogabrielsilva.github.io'`, `BASE_PATH = '/caixa-preta-pc-monitor/'`, `SITE_URL = SITE_ORIGIN + BASE_PATH` (com barra final), `REPO = 'oGabrielSilva/caixa-preta-pc-monitor'`, `REPO_URL`, `RELEASES_PAGE = REPO_URL + '/releases'`, `AUTHOR = { name: 'Gabriel Silva', url: 'https://github.com/oGabrielSilva' }`. `i18n/site-locales.ts` com `siteLocales` (`pt-BR` → `language: 'pt-BR'`, `en` → `language: 'en-US'`) e `defaultLocale = 'pt-BR'`.
- `nuxt.config.ts`: `extends: ['../PcMonitor.Brand']`, `modules: ['@nuxt/ui', '@nuxtjs/i18n', '@nuxt/eslint', '@nuxt/test-utils/module']`, `ssr: true`, `app.baseURL: BASE_PATH`, `i18n: { strategy: 'prefix_except_default', baseUrl: SITE_URL sem barra final, customRoutes: 'config', pages: { releases: { 'pt-BR': '/versoes', en: '/releases' } } }`, `nitro.prerender: { crawlLinks: true, routes: ['/', '/en', '/sitemap.xml', '/robots.txt', '/llms.txt'] }`, `colorMode.preference: 'dark'`.

- [ ] **Step 1: Testes que falham:**
  - `locales.spec.ts`: mesmas chaves em pt-BR e en (via `localeKeys` da camada) e nenhuma chave com ponto (via `dottedKeys`). Nenhum valor contém `PC Monitor` nem `@` seguido de domínio (`/\S+@\S+\.\S+/`).
  - `nuxt-config.spec.ts`: `config.app.baseURL === '/caixa-preta-pc-monitor/'`, `config.ssr === true`, `config.i18n.strategy === 'prefix_except_default'`.
- [ ] **Step 2:** `pnpm install && pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** criar o esqueleto. O `app.vue` tem `<UApp>` + `<NuxtPage>` e define `htmlAttrs.lang` por `useLocaleHead`. Copiar a política de supply chain do `pnpm-workspace.yaml` do painel e acrescentar `sharp: true` em `allowBuilds`. Escrever o `AGENTS.md` do site no mesmo formato do painel.
- [ ] **Step 4:** `pnpm lint && pnpm typecheck && pnpm test && pnpm generate`. Esperado: verde, e `.output/public/index.html` existe.
- [ ] **Step 5:** `pwsh scripts/Test-AgentDocs.ps1` passa.
- [ ] **Step 6: Commit** `feat(site): scaffold the static landing page app`

### Task 3: Dados das releases

**Files:**
- Create: `src/PcMonitor.Site/shared/utils/releases.ts`, `src/PcMonitor.Site/modules/github-releases.ts`, `test/unit/releases.spec.ts`, `test/fixtures/github-releases.json` (formato real da API: duas estáveis, uma pré-release mais nova, um rascunho, uma sem `.exe`)

**Interfaces:**
- Produces:
  ```ts
  interface ReleaseAsset { name: string; url: string; size: number }
  interface Release { version: string /* sem "v" */; tag: string; publishedAt: string /* ISO */; prerelease: boolean; notes: string /* markdown */; htmlUrl: string; installer: ReleaseAsset | null; checksum: ReleaseAsset | null }
  function mapReleases(raw: unknown[]): Release[]            // ignora drafts; mais nova primeiro
  function latestStable(releases: Release[]): Release | null // não pré-release e com installer
  function downloadHref(releases: Release[]): string         // installer.url da latestStable ou RELEASES_PAGE
  function fetchReleases(fetcher?: typeof fetch, token?: string): Promise<Release[]> // nunca lança; [] em erro
  ```
  O instalador é o asset `/^CaixaPreta-Setup-.+\.exe$/`, e o checksum é o mesmo nome + `.sha256`.
- O módulo `github-releases` chama `fetchReleases(fetch, process.env.GITHUB_TOKEN)` uma vez em `setup` e registra `addTemplate({ filename: 'releases.mjs', getContents })`, com alias `#releases` → `export const releases = [...]` e tipo em `addTypeTemplate`. Ele registra um aviso (`logger.warn`) quando cai em `[]`.

- [ ] **Step 1: Testes que falham** com a fixture: `mapReleases` devolve 4 itens (o draft sai), na ordem de `published_at` decrescente, com `version` sem "v". `latestStable` devolve a estável mais nova com `.exe` (não a pré-release). `downloadHref([])` é igual a `RELEASES_PAGE`. `fetchReleases` com um fetcher que devolve 404 → `[]`, com um fetcher que lança → `[]`, e com token envia `Authorization: Bearer <token>` e `Accept: application/vnd.github+json`. Uma nota com `<script>alert(1)</script>` continua texto em `notes` (a renderização segura é testada na Task 7).
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** implementar as funções e o módulo, e registrar o módulo em `nuxt.config.ts`.
- [ ] **Step 4:** `pnpm test && pnpm generate`. Esperado: verde. Hoje o repositório não existe no GitHub, então o build mostra o aviso e segue.
- [ ] **Step 5: Commit** `feat(site): read the GitHub releases at build time`

### Task 4: SEO, JSON-LD, sitemap, robots e llms.txt

**Files:**
- Create: `src/PcMonitor.Site/shared/utils/seo.ts`, `server/routes/sitemap.xml.ts`, `server/routes/robots.txt.ts`, `server/routes/llms.txt.ts`, `app/composables/usePageSeo.ts`, `app/utils/faq.ts`, `test/unit/seo.spec.ts`

**Interfaces:**
- Consumes: `SITE_URL`, `AUTHOR`, `REPO_URL` (Task 2) e `Release`, `latestStable`, `downloadHref` (Task 3).
- Produces:
  ```ts
  function absoluteUrl(path: string): string // '/' → SITE_URL; '/en/releases' → SITE_URL + 'en/releases'
  function softwareApplicationJsonLd(input: { description: string; locale: string; release: Release | null; screenshots: string[] }): object
  function faqPageJsonLd(items: { question: string; answer: string }[]): object
  function websiteJsonLd(locale: string): object
  function breadcrumbJsonLd(items: { name: string; path: string }[]): object
  function sitemapXml(pages: { path: string; alternates: Record<string, string> }[]): string
  function robotsTxt(): string // Allow: / + Sitemap: absoluteUrl('/sitemap.xml')
  ```
  `app/utils/faq.ts` exporta `FAQ_KEYS` (a lista ordenada das chaves i18n de pergunta e resposta), a única fonte usada pela seção visível e pelo JSON-LD. `usePageSeo({ titleKey, descriptionKey, path, image })` aplica `useSeoMeta` (title, description, `og:*`, `twitter:card = 'summary_large_image'`) e `useHead` (canonical + o `useLocaleHead` do i18n).
- `softwareApplicationJsonLd`: `@type: 'SoftwareApplication'`, `name: 'Caixa-Preta'`, `operatingSystem: 'Windows 11'`, `applicationCategory: 'UtilitiesApplication'`, `offers: { '@type': 'Offer', price: '0', priceCurrency: 'BRL' }`, `license: 'https://opensource.org/licenses/MIT'`, `author: { '@type': 'Person', ...AUTHOR }`, `codeRepository`/`sameAs: REPO_URL`. Com release, inclui `softwareVersion`, `downloadUrl`, `datePublished` e `fileSize`. Sem release, omite os quatro.

- [ ] **Step 1: Testes que falham:** `absoluteUrl` nos dois casos acima e em `'en'` sem barra. O JSON-LD não tem nenhuma string com `@gmail`. Sem release, `softwareVersion` fica indefinido. `faqPageJsonLd` tem `mainEntity.length === items.length` e cada item é `Question` com `acceptedAnswer.text`. O `sitemapXml` tem `<xhtml:link rel="alternate" hreflang="en" href=".../en/">` e `x-default`. O `robotsTxt` contém `Sitemap: https://ogabrielsilva.github.io/caixa-preta-pc-monitor/sitemap.xml`. As chaves de `FAQ_KEYS` existem nas duas línguas.
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** implementar. As rotas de servidor só chamam as funções puras e definem o `Content-Type`. O `llms.txt` (pt e en no mesmo arquivo) tem o resumo factual: o que é, para quem, o que grava, como diagnostica, privacidade, requisitos, licença e links.
- [ ] **Step 4:** `pnpm test && pnpm generate`. Esperado: `.output/public/sitemap.xml`, `robots.txt` e `llms.txt` existem.
- [ ] **Step 5: Commit** `feat(site): add structured data, sitemap, robots and llms.txt`

### Task 5: Imagens (capturas, banco de demonstração, OG)

**Files:**
- Create: `src/PcMonitor.Site/screenshots/DemoDatabase.cs` (app C# de arquivo único: `#:project ../../../tests/PcMonitor.Tests.Support/PcMonitor.Tests.Support.csproj`), `src/PcMonitor.Site/screenshots/README.md` (como refazer as capturas), `src/PcMonitor.Site/scripts/optimize-screens.mjs`, `src/PcMonitor.Site/scripts/build-og.mjs`, `public/screens/*.webp`, `public/og/og-pt-BR.png`, `public/og/og-en.png`, `public/icon.svg`, `public/favicon.ico`
- Test: `test/unit/images.spec.ts`

**Interfaces:**
- `dotnet run src/PcMonitor.Site/screenshots/DemoDatabase.cs -- <caminho.db>` cria um banco migrado com `DatabaseSeed`: cerca de 10 min de leituras normais, depois 40 s com o +12 V caindo de 12,19 para cerca de 11,2 V sob carga alta, o fim da gravação, um incidente (`Kernel-Power 41`) e `Diagnose` com o motor real. **Sem** o evento 1074 com nome de máquina/usuário.
- O painel é rodado sobre esse banco com `dotnet run --project src/PcMonitor.Dashboard -- --Dashboard:DatabasePath=<caminho.db> --Dashboard:Port=5281` (conferir em `DashboardOptions` se a porta é configurável assim. Se não for, documentar a alternativa no README das capturas).
- Capturas finais (WebP, larguras 1600 e 800): `overview`, `history`, `mini` (dados reais da porta 5280) e `incident` (banco de demonstração, porta 5281). Pelo menos a `overview` também no tema claro.
- `build-og.mjs`: SVG 1200×630 (fundo neutral-950, ícone da marca, "Caixa-Preta" e o slogan do idioma, linha de batimento teal) → PNG via resvg, usando a fonte do sistema (`loadSystemFonts: true`, família `Segoe UI`).

- [ ] **Step 1: Teste que falha** `images.spec.ts`: existem as 4 capturas × 2 larguras e os 2 PNG OG. Cada OG tem 1200×630 (lido do cabeçalho IHDR). O `public/icon.svg` é idêntico a `assets/brand/caixa-preta.svg`.
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** gerar o banco de demonstração e subir o painel nele. Capturar as 4 telas com o navegador embutido (`mcp__Claude_Browser__*`, janela em cerca de 1440×900, só o conteúdo do painel). **Conferir cada captura** contra nome de máquina, usuário e caminhos. Rodar `optimize-screens.mjs` (sharp) e `build-og.mjs`. Gerar o favicon com o mesmo esquema do `build-icons.mjs` do painel; se isso duplicar código, mover o gerador de ICO para a camada `Brand`.
- [ ] **Step 4:** `pnpm test`. Esperado: verde.
- [ ] **Step 5: Commit** `feat(site): add screenshots, demo database and Open Graph images`

### Task 6: Página inicial

Antes de começar, invocar `frontend-design:frontend-design`.

**Files:**
- Create: `app/pages/index.vue` (só composição), `app/components/home/{HomeHero,FlightRecorder,FeatureBento,ScreenGallery,EngineeringSection,StorySection,FaqSection}.vue`, `app/components/shared/{SiteHeader,SiteFooter,PulseLine,DownloadButton}.vue`, `app/layouts/default.vue`, as chaves em `i18n/locales/*.json`
- Test: `test/nuxt/home.spec.ts` (`// @vitest-environment nuxt`)

**Interfaces:**
- Consumes: `releases` de `#releases`, `latestStable`, `downloadHref` (Task 3), `usePageSeo`, `FAQ_KEYS`, `softwareApplicationJsonLd`, `faqPageJsonLd` e `websiteJsonLd` (Task 4), e `/screens/*.webp` e `/og/*.png` (Task 5).
- `DownloadButton` mostra "Baixar para Windows" + versão e tamanho (`MB`) quando há release, e "Ver versões no GitHub" quando não há.
- O `FlightRecorder` é uma linha do tempo estática, em SVG/HTML, com dados ilustrativos fixos no componente: sensores segundo a segundo, a queda, "religou" e o cartão de diagnóstico com as evidências. Leva a etiqueta "exemplo ilustrativo" e anima só com `prefers-reduced-motion: no-preference`.
- A ordem e o conteúdo das seções seguem o spec, na seção "Páginas → `/`".

- [ ] **Step 1: Testes que falham** (montando a página): exatamente um `h1`. O `DownloadButton` aponta para `RELEASES_PAGE` quando `releases` está vazio. A seção FAQ renderiza `FAQ_KEYS.length` perguntas. O head tem um `script[type="application/ld+json"]` com `SoftwareApplication`, outro com `FAQPage` e outro com `WebSite`. Todo `img` tem `alt` não vazio, `width` e `height`. A imagem `incident` tem legenda com "dados de exemplo".
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** construir as seções com componentes do Nuxt UI (`UPageHero`, `UPageSection`, `UPageGrid`/`UPageCard`, `UPageAccordion` ou `UAccordion`, `UButton`, `UBadge`, `UColorModeButton`, `ULocaleSelect`) e ícones Material Symbols. Escrever os textos pt-BR e en fiéis ao README, às ADRs e à arquitetura. A História é a frase única do spec.
- [ ] **Step 4:** `pnpm test && pnpm generate`, depois `pnpm preview` (ou servir `.output/public` sob `/caixa-preta-pc-monitor/`) e revisar no navegador embutido em 1440 px e em 375 px, nos temas claro e escuro. Sem rolagem horizontal, foco visível no teclado e contraste AA.
- [ ] **Step 5: Commit** `feat(site): build the home page`

### Task 7: Página de versões

**Files:**
- Create: `app/pages/releases.vue`, `app/components/releases/{ReleaseList,ReleaseNotes,ReleasesEmpty}.vue`, as chaves em `i18n/locales/*.json`
- Test: `test/nuxt/releases.spec.ts`

**Interfaces:**
- Consumes: `releases`, `Release` (Task 3), `usePageSeo` e `breadcrumbJsonLd` (Task 4).
- `ReleaseNotes` renderiza o markdown com `MDC`/`@nuxtjs/mdc` **ou** com um conversor mínimo que escapa HTML. Nunca com `v-html` de texto sem escape.
- Usa `UChangelogVersions`/`UChangelogVersion`: versão, data formatada no locale, selo "pré-release", notas e botões `.exe` (com tamanho) e `.sha256`.

- [ ] **Step 1: Testes que falham:** com a fixture, aparecem 4 versões, na ordem, com o selo na pré-release. Uma nota com `<script>` não gera elemento `script` no DOM. Com `[]`, aparece o `ReleasesEmpty` com um link para `RELEASES_PAGE`. O head tem um `BreadcrumbList`. Em `/en/releases`, o canonical é `https://ogabrielsilva.github.io/caixa-preta-pc-monitor/en/releases`.
- [ ] **Step 2:** `pnpm test`. Esperado: FAIL.
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** `pnpm test && pnpm generate`. Esperado: `.output/public/versoes/index.html` e `.output/public/en/releases/index.html` existem.
- [ ] **Step 5: Commit** `feat(site): add the release history page`

### Task 8: Workflows e documentação

**Files:**
- Create: `.github/workflows/pages.yml`, `docs/adr/0021-landing-page-and-shared-theme.md`
- Modify: `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.jscpd.json` (ignorar `**/public/**`, se as imagens atrapalharem), `docs/adr/README.md`, `docs/architecture/overview.md`, `docs/architecture/frontend.md`, `README.md` (link para a LP nas duas línguas)

**Interfaces:**
- `pages.yml`: `on: push (branches: [main], paths: ['src/PcMonitor.Site/**', 'src/PcMonitor.Brand/**', 'assets/brand/**', '.github/workflows/pages.yml'])`, `workflow_dispatch`, `release: types: [published]`. `permissions: { contents: read, pages: write, id-token: write }`, `concurrency: { group: pages, cancel-in-progress: false }`. Job `build` (ubuntu-latest): checkout, pnpm/action-setup (`package_json_file: src/PcMonitor.Site/package.json`), setup-node (`.nvmrc`, cache pelo lockfile do site), `pnpm install --frozen-lockfile`, `pnpm generate` com `GITHUB_TOKEN: ${{ github.token }}` e `actions/upload-pages-artifact` (`path: src/PcMonitor.Site/.output/public`). Job `deploy`: `actions/deploy-pages`, `environment: github-pages`. Usar as mesmas versões maiores das actions que já estão no repositório e, para as novas, a versão mais recente.
- `release.yml`: `permissions` ganha `actions: write`, e o último passo é `gh workflow run pages.yml --ref main`.
- `ci.yml`: um bloco "Site" (install, lint, typecheck, test e generate em `src/PcMonitor.Site`), com `cache-dependency-path` listando os dois lockfiles.
- ADR 0021: contexto (portfólio, comunidade, quem tem o problema), a decisão (camada `Brand`, app `Site` SSG, releases no build, Pages, disparo pelo `release.yml`), as alternativas B e C do spec e as consequências (depende de o repositório ser público; o site se reconstrói a cada release).

- [ ] **Step 1:** escrever os workflows e validar a sintaxe com `actionlint`, se estiver disponível (`winget`/`go`). Se não estiver, revisar com cuidado contra a documentação da action.
- [ ] **Step 2:** escrever a ADR e atualizar a arquitetura, o índice de ADRs e o README.
- [ ] **Step 3:** `pwsh scripts/Test-AgentDocs.ps1` e `pnpm dlx jscpd@5.4.3` na raiz. Esperado: os dois passam.
- [ ] **Step 4: Commit** `ci: publish the landing page to GitHub Pages` e `docs: record the landing page decision (ADR 0021)`

### Task 9: Post do LinkedIn e verificação final

**Files:**
- Create: `docs/divulgacao/2026-11-26-post-linkedin.md`

- [ ] **Step 1:** escrever o post em pt-BR, em primeira pessoa, com cerca de 1.300 caracteres. Ele tem um gancho (o PC desligando sozinho), o que o app faz, duas ou três decisões técnicas (gravação à prova de queda com SQLite WAL, diagnóstico explicável por regras, três processos com privilégio mínimo), o convite para testar e contribuir, o link da LP e 3–5 hashtags. Marcar com `[...]` onde o dono pode acrescentar a própria história, e incluir a sugestão de imagem (`og-pt-BR.png` ou a captura `overview`). Também pode haver uma variante curta em inglês.
- [ ] **Step 2:** verificação completa:
  - `src/PcMonitor.Site`: `pnpm lint && pnpm typecheck && pnpm test && pnpm generate`.
  - `src/PcMonitor.Dashboard.Web`: o mesmo conjunto.
  - Raiz: `dotnet build PcMonitor.slnx`, `pwsh scripts/Test-AgentDocs.ps1` e `pnpm dlx jscpd@5.4.3`.
  - Em `.output/public`, `grep` por `gmail` não acha nada, e cada `index.html` tem 1 `<h1>`, `canonical`, `og:image` absoluto, `hreflang` (pt-BR, en e x-default) e os JSON-LD esperados.
  - Lighthouse (mobile) na prévia local: ≥ 95 nas quatro categorias, ou anotar no relatório o que ficou abaixo e por quê.
- [ ] **Step 3: Commit** `docs: draft the LinkedIn post for the launch`
- [ ] **Step 4:** relatório ao dono: o que foi feito, o caminho da LP gerada e o que ele precisa fazer para publicar (criar o repositório público `caixa-preta-pc-monitor`, `git push`, ativar Pages → "GitHub Actions", criar a tag `v1.0.0`). **Não executar nenhum desses passos.**
