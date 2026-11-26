# 0017 — Identidade do produto: Caixa-Preta

**Contexto.** O app era só "PC Monitor". O dono pediu um nome e um ícone e escolheu **Caixa-Preta**: como o gravador de voo, ele grava tudo o tempo todo para explicar a queda depois.

**Decisão.**
- Nome visível em tudo o que o usuário vê: título do painel e da janela, bandeja, painel fixo, menu Iniciar, nomes de exibição dos serviços ("Caixa-Preta (Coletor)", "Caixa-Preta (Painel)"), instalador.
- Ícone A (caixa escura com a linha de um batimento em teal), com uma única fonte: `assets/brand/caixa-preta.svg`. O ICO (16–256 px), o PNG e o favicon são gerados dela por `pnpm icons` (`@resvg/resvg-js`) e versionados.
- Os nomes internos ficam: projetos e namespaces `PcMonitor.*`, serviços `PcMonitor.Collector`/`PcMonitor.Dashboard`, dados em `%ProgramData%\PcMonitor`.
- Nomes de exibição só com ASCII nos scripts: o Windows PowerShell 5.1, usado pelo instalador, lê scripts sem BOM como ANSI.

**Alternativas.** Renomear tudo para `CaixaPreta.*` (quebraria a instalação atual, o banco e os caminhos sem ganho para o usuário); nomes "Pulso" ou "Sentinela" (descartados pelo dono).

**Consequências.** O código continua dizendo PC Monitor por dentro; o usuário só vê Caixa-Preta. Trocar o ícone é editar um SVG e rodar um comando.
