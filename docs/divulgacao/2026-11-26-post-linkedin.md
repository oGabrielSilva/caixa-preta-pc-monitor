# Rascunho: post do LinkedIn para o lançamento da Caixa-Preta

> Rascunho para o dono revisar. Nada foi publicado. O trecho entre `[...]` é para você completar ou apagar.

**Formato:** carrossel em PDF (post de documento). Arquivo: [`carrossel/carrossel-caixa-preta.pdf`](carrossel/carrossel-caixa-preta.pdf), com 8 slides de 1080 × 1350; as prévias ficam em `carrossel/png/`. A fonte é [`carrossel/slides.html`](carrossel/slides.html), e `pwsh docs/divulgacao/carrossel/build-carousel.ps1` gera o PDF de novo. No LinkedIn: **Começar publicação → Adicionar documento**, com o título "Caixa-Preta: por que meu PC desliga sozinho". O slide 4 usa dados de exemplo e diz isso.

**Link:** vai no primeiro comentário, logo depois de publicar (o slide 8 avisa). O LinkedIn entrega menos os posts com link externo no texto. Comentário sugerido: `Site e download: https://ogabrielsilva.github.io/caixa-preta-pc-monitor/ · Código: https://github.com/oGabrielSilva/caixa-preta-pc-monitor`

---

## Versão principal (pt-BR, cerca de 1.500 caracteres, limite do LinkedIn: 3.000)

Meu gato derrubou meu PC.

Ele é grande, e subiu no gabinete para pular para cima do guarda-roupa. Quando ele pulou, o gabinete não ficou na mesa: caiu de mais de 80 cm. Desde então o PC passou a reiniciar sozinho, e eu não sabia se o defeito era na placa-mãe ou na fonte.

O Windows só registra que houve um "desligamento inesperado". O motivo, não. Eu queria ver as tensões no momento da queda, então construí a Caixa-Preta: como o gravador de voo de um avião, ela grava os sensores do PC o tempo todo para explicar a queda depois.

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

👇 Link no primeiro comentário.

#dotnet #nuxt #opensource #windows #hardware

---

## Versão curta em inglês (opcional)

My cat knocked my PC off the desk. He climbed onto the case to jump on top of the wardrobe, and the case fell more than 80 cm. From then on it kept restarting on its own, and I could not tell whether the motherboard or the power supply was at fault. Windows only logs that a shutdown happened, not why. So I built Caixa-Preta ("black box"): a free, open-source (MIT) hardware monitor for Windows 11 that records temperatures, power supply rails and CPU power once per second, survives the power cut, and explains the likely cause of each unexpected shutdown, showing the evidence for and against. Everything stays on your machine.

Built with .NET 10 and Nuxt 4, with three least-privilege processes, a layered architecture checked by tests and every decision written down as an ADR.

🔗 https://ogabrielsilva.github.io/caixa-preta-pc-monitor/en/

#dotnet #opensource #windows

---

## Antes de postar

- [x] Repositório `oGabrielSilva/caixa-preta-pc-monitor` público, com o código enviado.
- [x] GitHub Pages ligado (Source: **GitHub Actions**).
- [x] Tag `v1.0.0` publicada e o instalador anexado à release.
- [ ] Página abrindo no link e o botão de download apontando para o instalador.
- [ ] Os trechos `[...]` preenchidos ou apagados.
- [ ] Prévia do link conferida, por exemplo no [Post Inspector do LinkedIn](https://www.linkedin.com/post-inspector/), para a imagem do Open Graph aparecer.
