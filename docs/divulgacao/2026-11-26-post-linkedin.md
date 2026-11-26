# Rascunho: post do LinkedIn para o lançamento da Caixa-Preta

> Rascunho para o dono revisar. Nada foi publicado. Os trechos entre `[...]` são para você completar com a sua história ou apagar. O link só funciona depois que o repositório estiver público e o GitHub Pages estiver ligado.

**Imagem sugerida:** `src/PcMonitor.Site/public/og/og-pt-BR.png` (1200 × 630). Outra opção é a captura `public/screens/overview-1600.webp`, que mostra o painel de verdade. Se for usar a captura do incidente, diga que são dados de exemplo.

---

## Versão principal (pt-BR, cerca de 1.500 caracteres, limite do LinkedIn: 3.000)

Meu PC caiu e passou a desligar e reiniciar sozinho. [Conte aqui como era: quando começou, se tinha tela azul, o que você já tinha tentado.]

O Windows só registra que houve um "desligamento inesperado". O motivo, não. Então eu construí a Caixa-Preta: como o gravador de voo de um avião, ela grava os sensores do PC o tempo todo para explicar a queda depois.

O que ela faz:
• Grava uma vez por segundo as temperaturas, as tensões +12 V, +5 V e +3,3 V da fonte, o consumo e a carga da CPU, a memória e os discos.
• Não perde os últimos segundos quando a energia cai (SQLite em modo WAL).
• Detecta cada desligamento inesperado no Log de Eventos do Windows.
• Dá um diagnóstico explicável da causa provável (fonte, superaquecimento, placa-mãe ou software), com o que pesou a favor e contra. Não é um laudo, é uma pista honesta.
• Tem um mini painel sempre visível que fica vermelho quando algo sai do normal.

Algumas decisões de que gostei:
• Três processos com o mínimo de privilégio: só o coletor lê o hardware; o painel apenas lê o banco.
• Arquitetura em camadas checada por testes, e cada decisão registrada numa ADR.
• Nada sai da máquina: sem nuvem, sem conta, sem telemetria.

É gratuita e de código aberto (MIT), para Windows 11. Se o seu PC também desliga sozinho, testa e me conta o que ela encontrou. Issues e PRs são bem-vindos.

[O que a Caixa-Preta mostrou no seu caso, se você quiser contar.]

🔗 https://ogabrielsilva.github.io/caixa-preta-pc-monitor/

#dotnet #nuxt #opensource #windows #hardware

---

## Versão curta em inglês (opcional)

My PC started shutting down on its own, and Windows only logs that it happened, not why. So I built Caixa-Preta ("black box"): a free, open-source (MIT) hardware monitor for Windows 11 that records temperatures, power supply rails and CPU power once per second, survives the power cut, and explains the likely cause of each unexpected shutdown, showing the evidence for and against. Everything stays on your machine.

Built with .NET 10 and Nuxt 4, with three least-privilege processes, a layered architecture checked by tests and every decision written down as an ADR.

🔗 https://ogabrielsilva.github.io/caixa-preta-pc-monitor/en/

#dotnet #opensource #windows

---

## Antes de postar

- [ ] Repositório `oGabrielSilva/caixa-preta-pc-monitor` público, com o código enviado.
- [ ] GitHub Pages ligado (Settings → Pages → Source: **GitHub Actions**) e a página abrindo no link acima.
- [ ] Tag `v1.0.0` publicada e o instalador anexado à release (o botão de download passa a apontar para ele).
- [ ] Os trechos `[...]` preenchidos ou apagados.
- [ ] Prévia do link conferida, por exemplo no [Post Inspector do LinkedIn](https://www.linkedin.com/post-inspector/), para a imagem do Open Graph aparecer.
