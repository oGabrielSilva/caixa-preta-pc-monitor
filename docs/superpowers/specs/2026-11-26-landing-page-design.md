# Landing page do Caixa-Preta — design

Data: 2026-11-26 · Status: rascunho para aprovação do dono

## Intenção

Uma página pública do Caixa-Preta que serve, nesta ordem, para:

1. **Portfólio** do dono: mostrar a engenharia (gravação à prova de queda de energia, diagnóstico explicável, arquitetura em três processos, testes, ADRs).
2. **Comunidade open source:** convidar a ler o código, abrir issues e contribuir (licença MIT).
3. **Quem tem o mesmo problema** (o PC desliga ou reinicia sozinho): explicar em linguagem simples o que o app faz e levar ao download.

Junto com a página vai um rascunho de post para o LinkedIn, que o dono revisa e publica.

## Objetivos

- Ser encontrada por buscadores (SEO) e citada por buscadores com IA (AEO/GEO): HTML estático, respostas diretas, fatos verificáveis e dados estruturados.
- Ter um visual novo, bonito e moderno, com a identidade do painel. **Não pode parecer um template genérico.**
- Mostrar o histórico de versões, como as páginas de apps desse tipo costumam fazer.
- Deixar o download a um clique, sempre apontando para a versão mais recente.

## Fora do escopo

- Domínio próprio, blog, documentação completa do usuário, formulário de contato, newsletter.
- Publicar, fazer push, tornar o repositório público ou postar no LinkedIn: isso fica com o dono.

## O que tornaria o trabalho um fracasso

- Página genérica de "hero + 3 cards + CTA".
- Qualquer rastreador, analytics, CDN de fontes ou de ícones. O produto promete que nada sai da máquina, e a página segue o mesmo espírito.
- O e-mail do dono, ou qualquer dado pessoal (nome da máquina, usuário do Windows), em texto, metadados ou imagens.
- Prometer o que o app não faz. Por exemplo, o diagnóstico é "causa provável", não certeza. O instalador não é assinado, e o FAQ diz isso.
- Tema copiado: o jscpd do CI falha com qualquer clone.

## Restrições

- Fica no mesmo repositório. Hospedagem no **GitHub Pages**, sem domínio próprio. O repositório será `oGabrielSilva/caixa-preta-pc-monitor`, então a URL base é `https://ogabrielsilva.github.io/caixa-preta-pc-monitor/` (Nuxt `app.baseURL = '/caixa-preta-pc-monitor/'`).
- **Nuxt 4 + Nuxt UI 4**, com as mesmas versões do `Dashboard.Web`, gerado de forma estática (`nuxt generate`, com SSR no build e sem servidor).
- Regras do `AGENTS.md`: código em inglês, documentação em português, sem duplicação (jscpd 0%), arquivos `AGENTS.md` + `CLAUDE.md` na nova pasta, nova ADR, `docs/architecture/` atualizado, testes primeiro.
- Fontes e ícones vêm no build (`@nuxt/fonts` baixa as fontes, ícones com `provider: 'none'`), como no painel.

## Abordagens consideradas

- **A — app novo `src/PcMonitor.Site` + camada de tema compartilhada + histórico vindo da API de Releases do GitHub no build.** *Escolhida pelo dono.* O histórico se mantém sozinho (o `release.yml` já usa `--generate-notes`), o painel e a LP ficam iguais sem cópia e a página é reconstruída a cada release. Custo: depende de o repositório ser público; antes da primeira release ela mostra "primeira versão em breve".
- **B — mesmo app, histórico num `CHANGELOG.md` com Nuxt Content.** Funciona com o repositório privado, mas cria duas fontes de verdade e exige atualização manual.
- **C — pré-renderizar a LP dentro do `Dashboard.Web`.** Descartada: o painel é um SPA local sem SSR, servido pelo ASP.NET.

## O que foi decidido

### Estrutura no repositório

- `src/PcMonitor.Brand/`: **camada Nuxt** (Nuxt layer) com o que é comum às duas UIs: o `app.config.ts` (cores e ícones Material Symbols), o CSS do tema (fonte Google Sans Flex, cantos retos) e a configuração de fontes e ícones. O `Dashboard.Web` passa a estendê-la (`extends`) e deixa de ter cópia própria. O comportamento do painel não muda.
- `src/PcMonitor.Site/`: app Nuxt da LP, que estende a camada. Ele tem `AGENTS.md` + `CLAUDE.md`, lint, typecheck e testes (Vitest), no mesmo padrão do `Dashboard.Web`.
- `.github/workflows/pages.yml`: build (`nuxt generate`) e deploy no GitHub Pages (`actions/deploy-pages`). Dispara em push para `main` que mexa em `src/PcMonitor.Site/**` ou `src/PcMonitor.Brand/**`, em `release: published` e manualmente (`workflow_dispatch`).
- O `ci.yml` passa a rodar lint, typecheck, testes e generate do site.
- Nova ADR `0021` (LP + camada de tema compartilhada) e atualização de `docs/architecture/` (overview e frontend).

### Idiomas

pt-BR é o idioma principal, em `/`, e o inglês fica em `/en/`, ambos com `@nuxtjs/i18n` (strategy `prefix_except_default`). Cada página tem `hreflang` (pt-BR, en, x-default) e `<html lang>` correto.

### Páginas

1. **`/` — página inicial** (seções na ordem):
   - **Hero:** o nome, uma frase direta ("Seu PC desligou sozinho? O Caixa-Preta grava os sensores segundo a segundo e explica a causa provável."), o botão de download (versão, tamanho e "Windows 11 x64") e um secundário "Ver no GitHub". A arte é uma composição com a captura real do painel num quadro, o mini painel flutuando por cima e a linha de batimento em teal do ícone animada atravessando a seção, respeitando `prefers-reduced-motion`.
   - **"Como uma caixa-preta de avião":** a seção que dá identidade à página. É uma linha do tempo de um desligamento: os sensores (+12 V, temperatura da CPU, consumo) segundo a segundo até a queda, depois o "religou" e, ao lado, o diagnóstico explicável com as evidências. Os dados são ilustrativos e estáticos, desenhados em SVG/HTML com as cores semânticas do tema, e a seção diz que são um exemplo.
   - **Recursos** numa grade irregular (bento), um bloco por capacidade: gravação 1×/s à prova de queda (SQLite WAL), detecção de desligamento inesperado, diagnóstico explicável (fonte, superaquecimento, placa-mãe, software), mini painel sempre visível que fica vermelho, exportação CSV/Excel/PDF e privacidade (nada sai da máquina).
   - **Galeria de telas:** capturas reais com legenda (visão geral, histórico, incidente com diagnóstico, mini painel).
   - **Engenharia / open source:** para o portfólio, com os três processos (diagrama simples), Clean Architecture com testes de arquitetura, .NET 10 + Nuxt 4, ADRs públicas e licença MIT. Tem links para o código, as ADRs e o estudo de arquitetura no GitHub.
   - **História:** curta, em primeira pessoa do dono, sem fatos inventados. Até o dono dar detalhes, é uma frase só: "nasceu porque o meu PC passou a desligar e reiniciar sozinho, e eu queria saber por quê". O mesmo limite vale para o post do LinkedIn, que marca com `[...]` onde o dono pode acrescentar detalhes.
   - **FAQ** (respostas diretas na primeira frase, para AEO), por exemplo: por que meu PC desliga sozinho; o que o Caixa-Preta faz; é grátis; os dados saem da máquina; por que o Windows mostra um alerta ao instalar (não assinado/SmartScreen); quais sensores ele lê; ele diagnostica com certeza; requisitos; como desinstalar e o que acontece com os dados.
   - **Rodapé:** versão atual, links (GitHub, versões, licença, issues), troca de idioma e de tema claro/escuro.
2. **`/versoes` (`/en/releases`) — histórico de versões:** `UChangelogVersions`, uma entrada por release com versão, data, notas (o markdown da release renderizado) e link do `.exe` e do `.sha256`. Sem releases, mostra um estado vazio: "a primeira versão está a caminho" e um link para o GitHub.

### Dados das releases

- No build, um módulo ou utilitário lê `GET https://api.github.com/repos/oGabrielSilva/caixa-preta-pc-monitor/releases` (público, sem token; no workflow usa `GITHUB_TOKEN` para evitar limite de requisições). O mapeamento (release → versão, data, notas, asset `.exe`, tamanho, `.sha256`) tem testes.
- Se a API falhar ou retornar 404 (repositório ainda privado, ou sem remoto), o build **não falha**: ele usa a lista vazia e mostra o estado "em breve".
- **Link de download:** a URL direta do asset `.exe` da release mais recente (`browser_download_url`), lida no build. Como a página é reconstruída a cada release, o link fica sempre atualizado sem mudar o `release.yml`. Sem release, o botão leva a `https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases`.

### SEO / AEO / GEO

- `useSeoMeta` em cada página: title, description, canonical absoluto, Open Graph (`og:type`, `og:url`, `og:image` 1200×630, `og:locale` + alternate), Twitter card `summary_large_image`.
- JSON-LD: `SoftwareApplication` (nome, `operatingSystem: Windows 11`, `applicationCategory: UtilitiesApplication`, `offers` preço 0, `softwareVersion`, `downloadUrl`, `license` MIT, autor `Person` só com o nome "Gabriel Silva" e o perfil do GitHub, `screenshot`), `FAQPage` com as mesmas perguntas do FAQ visível, `WebSite` e `BreadcrumbList` em `/versoes`.
- `sitemap.xml` (com alternates de idioma) e `robots.txt` gerados no build, mais `llms.txt` com um resumo factual do produto para IAs.
- HTML semântico: um `h1` por página, seções com `h2`, imagens com `alt` descritivo, `width`/`height` e `loading="lazy"` abaixo da dobra.
- Meta de qualidade: Lighthouse ≥ 95 em Performance, Acessibilidade, Boas práticas e SEO (mobile).

### Imagens

- **Capturas reais** do painel que roda localmente (`127.0.0.1:5280`), tiradas com o navegador embutido nos temas escuro e claro, se ficarem boas. Cada uma é revisada para não mostrar nome da máquina, usuário ou caminhos pessoais.
- **Origem dos dados (decisão do dono):** a visão geral, o histórico e o mini painel mostram os **dados reais** da máquina do dono. A **tela de incidente** é capturada do painel real rodando sobre um **banco de demonstração** separado (leituras sintéticas com queda de +12 V antes do desligamento), sem tocar no banco real. A legenda e o `alt` dessa imagem dizem "dados de exemplo". O script que gera esse banco fica no repositório (por exemplo, em `scripts/` ou `src/PcMonitor.Site/scripts/`) para a captura ser reproduzível. São salvas como WebP/AVIF otimizados em `src/PcMonitor.Site/public/screens/`, com `@nuxt/image` (provider estático `ipx` no build) ou arquivos já otimizados.
- **Imagem OG** 1200×630 por idioma, gerada a partir do `assets/brand/caixa-preta.svg` + título por script no build (resvg, como o `pnpm icons` do painel), e versionada.
- **Favicon/ícones** vindos da fonte única em `assets/brand/`.

### Post do LinkedIn

`docs/divulgacao/2026-11-26-post-linkedin.md`, em pt-BR, em primeira pessoa e no tom de quem constrói: o problema real (o PC desligando sozinho), o que foi construído, duas ou três decisões técnicas interessantes, o convite para testar e contribuir e o link da LP. Inclui uma sugestão de imagem (a OG ou uma captura). É só um rascunho para o dono revisar.

## Decisões tomadas pelo assistente na revisão (o dono pode corrigir)

- **Reconstruir o site a cada release:** uma release criada com o `GITHUB_TOKEN` não dispara outros workflows, então só o gatilho `release: published` não basta. Por isso o `release.yml` ganha um passo final `gh workflow run pages.yml` (o `workflow_dispatch` é permitido com o `GITHUB_TOKEN`) e a permissão `actions: write`. Não é preciso PAT.
- **Camada de tema sem workspace pnpm:** `src/PcMonitor.Brand` não tem dependências próprias (usa as do app que a estende) e é referenciada por caminho relativo (`extends: ['../PcMonitor.Brand']`). Cada app mantém o próprio lockfile. O `Build-Release.ps1` e o `release.yml` não mudam por causa da camada, e o build do instalador é testado depois da mudança.
- **Pré-releases:** aparecem no histórico com o selo "pré-release". O botão de download sempre aponta para a release estável mais recente. Rascunhos não aparecem.

## O que fica com quem constrói

- Composição exata e microinterações de cada seção, dentro da direção acima.
- Formato das imagens (WebP vs AVIF) e se usa `@nuxt/image` ou só arquivos otimizados.
- Como gerar o sitemap/robots (módulo `@nuxtjs/sitemap` ou rota de servidor pré-renderizada), desde que nada vá para um CDN em tempo de execução.
- O texto final de cada seção e do FAQ, fiel ao README, às ADRs e à arquitetura.
