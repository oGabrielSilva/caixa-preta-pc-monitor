# 0013 — Gráficos com o `UChart` do Nuxt UI

**Contexto.** O estudo previa o Unovis para os gráficos e o [ADR 0007](0007-nuxt-spa-static-with-nuxt-ui.md) deixou a escolha para o Plano 05. Os mockups aprovados pelo dono foram feitos com o `UChart`, que o Nuxt UI 4.13 passou a trazer (sobre `@tanstack/charts`).

**Decisão.** Usar só o `UChart` (linhas, áreas e barras), com `@tanstack/charts` fixado no `package.json`. Todo gráfico de série temporal passa por `components/shared/TimeSeriesChart.vue`, que monta as linhas com `utils/chart-rows.ts`: o eixo X do `UChart` é uma escala de pontos por rótulo, então os rótulos precisam ser únicos, e uma linha sem valores é inserida onde nada foi gravado (PC desligado), para a linha do gráfico se interromper. Cores de série são categorias (paleta crua: âmbar, céu, violeta), nunca as cores semânticas de status.

**Alternativas.** Unovis (segunda biblioteca de componentes visuais, visual diferente do resto); uPlot (mais rápido para séries longas, mas sem integração com o tema).

**Consequências.** Uma única biblioteca, com o mesmo tema claro/escuro dos componentes. A escala de pontos não é proporcional ao tempo: um buraco longo ocupa só uma posição no eixo; o rótulo e a quebra da linha mostram o buraco. Se o histórico ficar lento com muitos pontos, o plano B continua sendo o uPlot.
