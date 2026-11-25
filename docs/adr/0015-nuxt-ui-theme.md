# 0015 — Tema do Nuxt UI escolhido pelo dono

**Contexto.** Depois de ver o painel instalado, o dono montou um tema no editor de temas do Nuxt UI (ui.nuxt.com/theme) e pediu para usá-lo. O painel não pode buscar nada na internet ao abrir ([ADR 0007](0007-nuxt-spa-static-with-nuxt-ui.md), [ADR 0014](0014-read-api-sse-and-event-privacy.md)).

**Decisão.**
- **Cores:** primária `teal`, neutro `neutral`. As cores de status continuam do projeto e só pelo significado: `success` esmeralda (dentro da faixa), `warning` âmbar (atenção), `error` rosa (fora da faixa).
- **Forma:** raio 0 (cantos retos), fundos claro/escuro e pesos de fonte do tema, em `app/assets/css/main.css`; campos de formulário no estilo `subtle`, em `app/app.config.ts`.
- **Fontes:** Google Sans Flex (texto e títulos) e Google Sans Code (monoespaçada). O `@nuxt/fonts` (ativado pelo Nuxt UI) baixa os arquivos **no build** e os serve do próprio painel (`_nuxt/fonts`); pesos 400, 500, 700 e 800.
- **Ícones:** Material Symbols (arredondados) para os componentes do Nuxt UI e para as telas, empacotados com `@iconify-json/material-symbols`; o conjunto lucide saiu. Um teste garante que todo ícone usado existe no conjunto empacotado.

**Alternativas.** Manter o tema padrão (sky/zinc, lucide); carregar as fontes do Google em tempo de execução (quebraria o funcionamento offline e enviaria requisições para fora).

**Consequências.** O build (máquina de desenvolvimento e CI) precisa de internet para baixar as fontes; o painel instalado não. Ícones novos devem ser escolhidos no Material Symbols.
