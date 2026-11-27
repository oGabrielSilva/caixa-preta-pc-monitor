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

## Telas ✅

| Rota | Mostra | Dados |
|---|---|---|
| `/` Ao vivo | estado da gravação, CPU, +12 V, consumo e carga com minigráficos, tendência de 10 min, réguas das tensões com a faixa ATX, Vcore, ventoinhas, memória, últimas quedas | `useLive` (retrato + SSE), `useIncidents` |
| `/incidents` | resumo (total, 24 h, logo após ligar, em hibernação), quedas por dia, tabela | `useIncidents` |
| `/incidents/:id` | causa mais provável e confiança, placar das hipóteses, linha do tempo, os 2 min antes da queda, o que pesou, limites da análise | `useIncident` |
| `/mini` | painel fixo do app da bandeja: CPU, carga e tensões numa linha, ponto de gravação | `useLive`, `useUnseenIncident` |
| `/history` | sensor × período (1 h, 24 h, 7 dias, 14 dias) na URL, mínimo/média/máximo, quedas do período | `useHistorySensors`, `useHistory`, `useHistorySelection` |

```mermaid
flowchart LR
    P[pages/*.vue<br/>só composição] --> C[components/live · incidents · history · shared]
    P --> Q[composables/use*<br/>único acesso à API]
    C --> U[utils/*<br/>funções puras testadas]
    Q --> API[api/client.ts + schema.d.ts gerado]
    Q --> R[api-result.ts<br/>dados ou problema: storage.*, http.*, network]
```

- **Faixas e limites** chegam da API (`SensorResponse.range`, `limit`, `critical`), calculados pelo C# a partir das mesmas constantes do diagnóstico. O frontend nunca repete um limiar.
- **Textos do diagnóstico:** cada `FindingCode` tem uma chave em `utils/finding-keys.ts` (`satisfies Record<FindingCode, string>`): um código novo no C# quebra o typecheck até ganhar texto em pt-BR e en. Parâmetros chegam como texto invariante e são formatados no idioma (`utils/findings.ts`).
- **Gráficos:** só via `shared/TimeSeriesChart.vue` ([ADR 0013](../adr/0013-charts-with-nuxt-ui-uchart.md)).
- **Cores:** `success`/`warning`/`error` só para status de leitura (`utils/sensor-status.ts`); séries de gráfico usam a paleta crua.
- **Layout:** variante *inset* do `USidebar` (`layouts/default.vue`): navegação sobre o fundo, recolhível até os ícones; cada página é um cartão com a barra de título e o botão do menu (`shared/Page.vue`).
- **Tema:** o escolhido pelo dono ([ADR 0015](../adr/0015-nuxt-ui-theme.md)): teal/neutral, cantos retos, Google Sans Flex, ícones Material Symbols; fontes e ícones empacotados no build. Fica na camada compartilhada `src/PcMonitor.Brand`, a mesma da landing page (abaixo).

## Exportação ✅

```mermaid
flowchart LR
    P[Botão Exportar<br/>shared/ExportMenu.vue] --> C[composables/useExport]
    C --> T[utils/export/tables.ts<br/>ExportTable com os textos da tela]
    T --> CSV[csv.ts<br/>BOM, CRLF, ; em pt-BR]
    T --> X[exceljs<br/>carregado sob demanda]
    T --> PDF[incident-report.ts → pdfmake<br/>carregado sob demanda]
    CSV --> D[download.ts<br/>caixa-preta-tipo-data.ext]
    X --> D
    PDF --> D
```

Quedas: CSV, Excel e PDF; uma queda: PDF com a telemetria; histórico: CSV e Excel. Decisão em [ADR 0019](../adr/0019-export-in-frontend.md).

## Landing page ✅

Página pública em `src/PcMonitor.Site` ([ADR 0021](../adr/0021-landing-page-and-shared-theme.md)): Nuxt 4 + Nuxt UI renderizado em HTML no build e publicado no GitHub Pages, em `https://ogabrielsilva.github.io/caixa-preta-pc-monitor/` (pt-BR em `/`, inglês em `/en/`). Convenções: [AGENTS.md do site](../../src/PcMonitor.Site/AGENTS.md).

```mermaid
flowchart LR
    B[src/PcMonitor.Brand<br/>camada: app.config, brand.css,<br/>fontes e ícones] --> D[PcMonitor.Dashboard.Web<br/>SPA local]
    B --> S[PcMonitor.Site<br/>landing page]
    GH[(API de Releases<br/>do GitHub)] -->|uma vez no build| M[modules/github-releases.ts<br/>#releases]
    M --> S
    S -->|nuxt generate| H[HTML estático<br/>+ sitemap.xml, robots.txt, llms.txt]
    H -->|pages.yml| P[GitHub Pages]
    R[release.yml<br/>tag v*] -->|gh workflow run| P
```

| Rota | Mostra |
|---|---|
| `/` e `/en` | o problema e o download, a linha do tempo de uma queda (exemplo ilustrativo) com o diagnóstico, recursos, capturas, engenharia, história e FAQ |
| `/versoes` e `/en/releases` | cada release com data, notas (texto escapado, só links http/https), instalador e hash SHA-256 |

- **Releases:** lidas da API pública no build. Se a leitura falhar, o build não falha: a página mostra que a primeira versão está a caminho, e o download leva à página de releases. O botão de download da home sempre leva à página de versões do site, de onde o `.exe` é baixado.
- **SEO/AEO/GEO:** canonical e `hreflang` absolutos montados por `shared/utils/seo.ts`; Open Graph por idioma; JSON-LD `SoftwareApplication`, `FAQPage` (as mesmas perguntas do FAQ visível), `WebSite` e `BreadcrumbList`.
- **Imagens:** capturas do painel de verdade; a do incidente vem de um banco de demonstração e diz "dados de exemplo" ([como refazer](../../src/PcMonitor.Site/screenshots/README.md)).
