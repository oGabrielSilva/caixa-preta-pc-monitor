# Plano 06 — Caixa-Preta: app na bandeja, painel fixo, exportação, memória e discos, instalador

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** transformar o PC Monitor na **Caixa-Preta**: um app com nome e ícone próprios, que vive na bandeja do sistema, abre o painel numa janela nativa (WPF + WebView2), mantém um **painel fixo pequeno** sempre visível com os números principais (que ficam vermelhos quando algo sai do normal), exporta os dados (CSV, Excel, PDF), mostra memória e discos, e se instala por um único `.exe`.

**Architecture:** os dois serviços continuam como estão (coletor grava, painel lê e serve API + SPA em `127.0.0.1:5280`). Entra um terceiro processo, **`PcMonitor.Tray`** (WPF, sessão do usuário, inicia com o Windows): janela principal com WebView2 apontando para o painel, o **painel fixo** (uma segunda janela pequena, sempre por cima, também em WebView2, abrindo a rota `/mini` do próprio painel) e o ícone na bandeja. O painel fixo reaproveita o que o painel já tem: o fluxo ao vivo (SSE), as faixas e limites que vêm da API e a regra de cor (`statusOf`); nenhuma regra é reescrita em C#. Sem notificações: o vermelho no painel fixo substitui os avisos. A exportação acontece no frontend, que já tem os dados e os textos traduzidos (uma única fonte para os textos do diagnóstico). O instalador (Inno Setup) leva tudo publicado como *self-contained* e chama os mesmos scripts PowerShell que já instalam os serviços.

**Tech Stack:** .NET 10 (WPF, `Microsoft.Web.WebView2`, `System.Windows.Forms.NotifyIcon`) · LibreHardwareMonitor (armazenamento) · Nuxt 4 + Nuxt UI · `exceljs` e `pdfmake` (carregados só ao exportar) · `@resvg/resvg-js` (gera os ícones no build) · Inno Setup 6 · xUnit v3, Shouldly, Vitest.

**Spec:** pedidos do dono em 24/11/2026: app em WebView2 + WPF na bandeja; nome e ícone (escolhidos: **Caixa-Preta**, ícone **A** "caixa + pulso"); instalar por um `.exe` (release); exportar em Excel, CSV e PDF; monitorar memória e armazenamento; **(25/11) um painel pequeno fixo na tela, como a janelinha do Teams numa reunião, com poucos números que ficam vermelhos quando algo sai do normal, no lugar das notificações**. Mais o que já vale: [estudo](../estudos/2026-11-24-estudo-arquitetura.md), ADRs 0003, 0007, 0014, 0015, 0016.

## Fatos verificados (24/11/2026)

- O coletor já grava memória: `/ram/load/0` (uso da RAM), `/vram/load/1` e a temperatura dos pentes (`/memory/dimm/1` e `/3`). O armazenamento não é lido: `LhmSensorSource` liga só CPU, placa e memória (`IsStorageEnabled` desligado). As chaves dos discos nesta máquina ainda são desconhecidas (Task 2 começa por descobri-las).
- Os scripts de instalação exigem PowerShell 7 (`#Requires -Version 7`); um PC comum só tem o Windows PowerShell 5.1.
- Não há dados de mock no pacote de produção; o seed fica em `tests/`.
- O WebView2 Runtime vem com o Windows 11.

## Global Constraints

- Tudo dos planos anteriores continua valendo (camadas, CPM, warnings como erro, testes antes, jscpd 0, inglês no código, português nos docs, `AGENTS.md` + `CLAUDE.md` em cada projeto novo).
- **Nome do produto:** "Caixa-Preta" em tudo o que o usuário vê (título da janela e do painel, bandeja, painel fixo, menu Iniciar, nomes de exibição dos serviços, instalador, "Aplicativos instalados"). Os nomes internos (`PcMonitor.*`, serviços `PcMonitor.Collector`/`PcMonitor.Dashboard`, `%ProgramData%\PcMonitor`) **não mudam**: renomeá-los quebraria a instalação atual e o banco sem ganho para o usuário.
- **Ícone:** uma única fonte, `assets/brand/caixa-preta.svg` (ícone A). Todos os outros formatos (ICO multi-tamanho, PNG, favicon) são gerados dela por script e versionados.
- **Nada sai da máquina** em tempo de execução: o app da bandeja só fala com `127.0.0.1:5280`.
- **Painel fixo** (rota `/mini`, janela do app da bandeja):
  - Sempre por cima das outras janelas, sem barra de título nem botão na barra de tarefas, arrastável, posição lembrada entre execuções; começa no canto inferior direito, acima da bandeja.
  - Pequeno: cerca de 240 × 132 px. Mostra só **CPU °C, carga da CPU %, +12 V, +5 V, +3,3 V** e uma bolinha de gravação. Sem gráficos.
  - Cor dos números pela mesma regra do painel (`statusOf`, faixas e limites da API): normal na cor do texto, **vermelho** fora do normal (rail fora da faixa ATX, CPU ≥ 100 °C), âmbar para atenção (CPU no limite de projeto de 95 °C). Sem leitura: "—".
  - Bolinha: verde gravando; **vermelha** se o coletor parou (`Stale`/`Stopped`) ou o painel não responde.
  - Queda nova desde a última vez que o dono abriu Incidentes: um ícone vermelho pequeno no painel fixo; clicar abre a queda.
  - Clicar no painel fixo abre a janela principal. Menu da bandeja: Abrir · Mostrar painel fixo (marcável, ligado por padrão) · Iniciar com o Windows · Sair.
- **Exportação** no frontend, a partir dos dados da API e dos textos do i18n: CSV (UTF-8 com BOM, `;` como separador no pt-BR para abrir certo no Excel brasileiro), Excel `.xlsx`, PDF. As bibliotecas só são baixadas pelo navegador quando o usuário exporta (import dinâmico).
- **Instalador:** um `CaixaPreta-Setup-<versão>.exe`, exige administrador, instala .NET embutido (*self-contained*, win-x64), instala ou atualiza os três componentes sem perder dados, instala o PawnIO via winget quando houver internet (ADR 0010), cria o atalho do menu Iniciar e a entrada "iniciar com o Windows" do app, e desinstala tudo menos os dados (pergunta se quer apagar os dados). Versão do produto: **1.0.0**. Sem assinatura de código (o Windows mostra o aviso do SmartScreen; documentado).

## Review Focus

1. **Painel indisponível quando o app da bandeja abre** (serviço ainda iniciando no boot, ou parado): a janela principal mostra uma página local de espera e tenta de novo; o painel fixo mostra a bolinha vermelha e "—" e volta sozinho quando o painel responde; nada trava nem fecha. Testes nas Tasks 3 e 4.
2. **Painel fixo fora da tela** (monitor desligado, resolução ou escala do Windows mudou desde a última posição salva): ele volta para o canto inferior direito do monitor principal. Teste na Task 4.
3. **Segunda instância do app** (usuário clica no atalho com o app já na bandeja): não abre outro processo; mostra a janela existente. Teste na Task 4.
4. **Atualizar por cima de uma instalação antiga** (a feita pelos scripts dos Planos 02–05, com serviços rodando e banco com dados): o instalador para os serviços, troca os binários, mantém o banco e o histórico, e tudo volta a rodar. Verificação na Task 7.
5. **Exportar com dados ausentes** (queda sem diagnóstico ou sem telemetria, sensor sem leituras, período sem dados): o arquivo sai mesmo assim, com "—" ou a aba vazia com cabeçalho, sem erro. Testes na Task 5.

---

### Task 1: Identidade Caixa-Preta (nome e ícone)

**Files:**
- Create: `assets/brand/caixa-preta.svg` (o ícone A, desenho final), `assets/brand/AGENTS.md`/`CLAUDE.md`
- Create: `src/PcMonitor.Dashboard.Web/scripts/build-icons.mjs` (devDependency `@resvg/resvg-js`): gera `assets/brand/caixa-preta.ico` (16, 20, 24, 32, 40, 48, 64, 256 px, entradas PNG), `assets/brand/caixa-preta-256.png`, `src/PcMonitor.Dashboard.Web/public/favicon.ico` e `public/icon.svg`; script `pnpm icons`
- Modify: `i18n/locales/*.json` (`app.title` = "Caixa-Preta"), `layouts/default.vue` (ícone da marca no lugar do chip), `nuxt.config.ts` (favicon), `scripts/Install-*.ps1` (nomes de exibição "Caixa-Preta — Coletor"/"Caixa-Preta — Painel", atalho "Caixa-Preta")
- Create: `docs/adr/0017-product-identity.md`
- Test: `test/unit/brand.spec.ts` (o título do app é "Caixa-Preta" nos dois idiomas; `public/favicon.ico` existe e começa com a assinatura ICO); `tests/PcMonitor.Architecture.Tests` não muda

- [ ] **Step 1:** teste que falha (`brand.spec.ts`) → `corepack pnpm test` FAIL.
- [ ] **Step 2:** desenhar o SVG final a partir do rascunho A (grade de 256, traço ≥ 2 px visível em 16 px), gerar os formatos com `corepack pnpm icons`, aplicar nome e ícone.
- [ ] **Step 3:** `corepack pnpm test`, `lint`, `typecheck` PASS; conferir o favicon e o título no navegador.
- [ ] **Step 4:** commit `feat: name the product Caixa-Preta and add its icon`.

---

### Task 2: Memória e discos

**Files:**
- Modify: `src/PcMonitor.Infrastructure.Sensors.Lhm/LhmSensorSource.cs` (`IsStorageEnabled = true`), `Profiles/*.json` (excluir o que não interessa dos discos, se houver ruído)
- Modify: `src/PcMonitor.Domain/Sensors/SensorRole.cs` (+ `MemoryLoad`), perfil da placa (`/ram/load/0` → `MemoryLoad`)
- Create: `src/PcMonitor.Domain/Sensors/StorageReading.cs`: classifica sensores de disco pela chave/tipo da LibreHardwareMonitor (`/nvme/N`, `/ssd/N`, `/hdd/N`): temperatura, vida restante (%), espaço usado (%)
- Modify: `src/PcMonitor.Dashboard/Api/Sensors/SensorPresentation.cs`: limites de disco (temperatura ≥ 70 °C atenção; vida restante ≤ 10 % fora da faixa) e `group` do disco no `SensorResponse` (`Storage`, com índice do disco)
- Create: `src/PcMonitor.Dashboard.Web/app/components/live/{MemoryCard.vue, StorageCard.vue}`, `app/utils/storage-readings.ts`
- Test: `tests/PcMonitor.Domain.Tests/Sensors/StorageReadingTests.cs`, `tests/PcMonitor.Dashboard.Tests/Api/LiveEndpointsTests.cs` (disco com limites), `test/unit/storage-readings.spec.ts`, `test/nuxt/StorageCard.spec.ts`

- [ ] **Step 1: Descobrir as chaves reais.** Rodar `spikes/SensorProbe` como administrador com armazenamento ligado e registrar as chaves e nomes dos discos desta máquina no resultado do plano (o dono roda o comando elevado; o Claude lê a saída colada).
- [ ] **Step 2:** testes que falham: `StorageReading` classifica `/nvme/0/temperature/0`, `/nvme/0/level/…` (vida restante), `/nvme/0/load/…` (espaço usado) e ignora o resto; `SensorPresentation` dá `limit` 70 °C à temperatura do disco e `range` mínimo 10 % à vida restante; `storage-readings.ts` agrupa por disco; `StorageCard` mostra "—" sem leitura.
- [ ] **Step 3:** implementar. Ao vivo ganha "Memória" (uso da RAM em %, com minigráfico) e "Discos" (um bloco por disco: temperatura, vida restante, espaço usado). O Histórico já lista os sensores novos sem mudança.
- [ ] **Step 4:** `dotnet test --solution PcMonitor.slnx`, testes do frontend, lint, typecheck PASS.
- [ ] **Step 5:** commit `feat: monitor memory load and storage health`.

---

### Task 3: Painel fixo no frontend (rota `/mini`)

**Files:**
- Create: `app/layouts/mini.vue` (sem barra lateral, fundo do tema, sem rolagem), `app/pages/mini.vue` (só composição), `app/components/mini/{MiniPanel.vue, MiniValue.vue, MiniStatusDot.vue}`
- Create: `app/utils/mini-readings.ts`: escolhe os cinco valores pelos papéis (`CpuTemperature`, `CpuLoad`, `Rail12V`, `Rail5V`, `Rail3V3`) e a cor de cada um com `statusOf`
- Create: `app/utils/seen-incidents.ts` + `app/composables/useUnseenIncident.ts`: guarda no `localStorage` o maior id de queda já visto (a página Incidentes e o detalhe marcam como visto); o painel fixo mostra o ícone quando há uma mais nova. Falha do `localStorage` = nada a mostrar, nunca erro.
- O painel fixo usa o mesmo `useLive` do Ao vivo; i18n
- **Mockup primeiro:** duas variações do painel fixo (em linha e em grade) mostradas ao dono no navegador antes de fechar o visual.
- Test: `test/unit/mini-readings.spec.ts`, `test/unit/seen-incidents.spec.ts`, `test/nuxt/MiniPanel.spec.ts`

- [ ] **Step 1: Testes que falham**

```ts
it('picks cpu, load and the three rails in a fixed order')
it('marks a rail outside the ATX range red and the cpu at 95 °C amber')     // statusOf: error / warning
it('shows — for a sensor without a reading or missing on the board')
it('turns the recording dot red when the recording is stale or stopped, or the dashboard is unreachable')
it('flags an incident newer than the last one seen')                         // visto 16, existe 17 → ícone
it('treats unavailable storage as nothing new')                              // localStorage lança → sem ícone, sem erro
```

- [ ] **Step 2:** FAIL. **Step 3:** mockups → aprovação do dono → implementar. **Step 4:** testes, lint, typecheck, generate PASS; conferir `/mini` no navegador.
- [ ] **Step 5:** commit `feat: add the compact always-visible panel view`.

---

### Task 4: App da bandeja (`PcMonitor.Tray`)

**Files:**
- Create: `src/PcMonitor.Tray/` (`net10.0-windows`, WPF + `UseWindowsForms` para o `NotifyIcon`, `ApplicationIcon` = `caixa-preta.ico`, `AssemblyName` = `CaixaPreta`):
  - `App.xaml(.cs)`; `Windows/MainWindow.xaml(.cs)` (WebView2 no painel)
  - `Windows/MiniWindow.xaml(.cs)` (WebView2 em `/mini`, `Topmost`, `WindowStyle=None`, `ShowInTaskbar=False`, fundo transparente, arrastável)
  - `Windows/WindowPlacement.cs` (posição salva e trazida de volta para dentro da área de trabalho visível)
  - `Tray/TrayIcon.cs`; `Startup/SingleInstance.cs` (mutex + pipe nomeado para pedir à instância aberta que mostre a janela); `Startup/RunAtLogin.cs` (chave `HKCU\…\Run`)
  - `Settings/TraySettings.cs` (JSON em `%LocalAppData%\CaixaPreta`: painel fixo ligado e posição)
  - `Resources/Strings.resx` (pt-BR) + `Strings.en.resx` (menu da bandeja); `Assets/offline.html` (página de espera embutida); `AGENTS.md`, `CLAUDE.md`
- Modify: `PcMonitor.slnx`, `Directory.Packages.props` (`Microsoft.Web.WebView2`), testes de arquitetura (o app não depende de nenhum projeto do PC Monitor: só fala HTTP com o painel)
- Test: `tests/PcMonitor.Tray.Tests/` (`WindowPlacementTests`, `TraySettingsTests`, `SingleInstanceTests`, `RunAtLoginTests` numa chave de teste)

**Comportamento:**
- Ao iniciar (com o Windows): ícone na bandeja ("Caixa-Preta") e o painel fixo, se ligado; a janela principal só abre quando o dono pede.
- Clique no painel fixo ou "Abrir" → janela principal. Fechar a janela principal esconde; "Sair" encerra o app (os serviços continuam gravando).
- As duas janelas compartilham o mesmo ambiente do WebView2 (`%LocalAppData%\CaixaPreta\WebView2`), então o painel fixo sabe quais quedas já foram vistas na janela principal.
- Links externos abrem no navegador padrão; sem menu de contexto nem ferramentas de desenvolvedor.
- Porta do painel: `Dashboard:Port` do `appsettings` instalado, padrão 5280.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void A_saved_position_outside_every_screen_moves_back_to_the_primary_work_area()
[Fact] public void A_saved_position_inside_a_screen_is_kept()
[Fact] public void Settings_default_to_the_mini_panel_shown_and_survive_a_restart()
[Fact] public void A_second_instance_asks_the_first_to_show_its_window()
[Fact] public void Run_at_login_adds_and_removes_the_entry()
```

- [ ] **Step 2:** FAIL. **Step 3:** implementar. **Step 4:** `dotnet test --solution PcMonitor.slnx` PASS; rodar o app apontando para o painel instalado: painel fixo por cima de outras janelas, arrastar, reiniciar o app e ver a posição mantida, janela principal, menu da bandeja.
- [ ] **Step 5:** commit `feat: add the Caixa-Preta tray app with the main window and the always-on-top panel`.

---

### Task 5: Exportar (CSV, Excel, PDF)

**Files:**
- Create: `app/utils/export/{csv.ts, workbook.ts, incident-report.ts, file-name.ts}` (montagem pura das linhas e do documento), `app/composables/useExport.ts` (import dinâmico de `exceljs`/`pdfmake`, gera o `Blob` e baixa), `app/components/shared/ExportMenu.vue` (`UDropdownMenu` com CSV · Excel · PDF)
- Modify: páginas Incidentes (exporta a lista + diagnósticos), detalhe da queda (PDF da queda), Histórico (CSV/Excel do sensor e período); i18n
- Test: `test/unit/export/{csv, workbook, incident-report}.spec.ts`, `test/nuxt/ExportMenu.spec.ts`

**Conteúdo:**
- **CSV:** Incidentes = uma linha por queda (horário, precisão, causa provável, confiança, sinais); Histórico = uma linha por balde (início, mínimo, média, máximo, unidade). Datas no fuso local, números no formato do idioma.
- **Excel:** abas "Quedas", "Diagnóstico" (uma linha por evidência/observação/limitação, com o texto traduzido) e "Leituras" (Histórico: o sensor e o período escolhidos).
- **PDF:** relatório de uma queda (horário, causa provável e confiança, placar das hipóteses, o que pesou, limites da análise, linha do tempo e o gráfico dos 2 minutos antes, desenhado como vetor) e relatório geral (resumo + tabela das quedas).
- Nome do arquivo: `caixa-preta-<tipo>-<AAAA-MM-DD-HHmm>.<ext>`.

- [ ] **Step 1: Testes que falham**

```ts
it('writes incidents as CSV with a BOM, ; separators and local numbers')   // pt-BR
it('quotes values that contain the separator or quotes')
it('leaves the diagnosis columns as — for an undiagnosed incident')
it('builds the three workbook sheets with headers even when empty')
it('puts the translated findings and the telemetry chart in the incident report')
it('omits the chart and says so when there is no telemetry')
```

- [ ] **Step 2:** FAIL. **Step 3:** implementar. **Step 4:** testes, lint, typecheck, generate PASS; exportar os três formatos no navegador e abrir os arquivos.
- [ ] **Step 5:** commit `feat: export incidents and history to CSV, Excel and PDF`.

---

### Task 6: Instalador em um `.exe`

**Files:**
- Modify: `scripts/Install-Collector.ps1`, `Install-Dashboard.ps1`, `Uninstall-*.ps1`, `PcMonitorService.psm1`: compatíveis com **Windows PowerShell 5.1** (sem `#Requires -Version 7`; nada específico do 7) e parâmetro `-InPlace` (os binários já estão no destino; não copia)
- Create: `scripts/Install-Tray.ps1`/`Uninstall-Tray.ps1` (atalho "Caixa-Preta" no menu Iniciar de todos os usuários e entrada de inicialização em `HKLM\…\Run`)
- Create: `installer/CaixaPreta.iss` (Inno Setup 6: assistente em português, ícone, licença MIT, `PrivilegesRequired=admin`, `[Files]` dos três publishes, `[Run]` dos scripts com `powershell.exe -NoProfile -ExecutionPolicy Bypass`, desinstalação pelos scripts `Uninstall-*` e pergunta se apaga `%ProgramData%\PcMonitor`), `installer/AGENTS.md`/`CLAUDE.md`
- Create: `scripts/Build-Release.ps1` (publica os três em *self-contained* win-x64 com a versão, roda `ISCC.exe`, gera `artifacts/release/CaixaPreta-Setup-1.0.0.exe` e o SHA-256)
- Create: `.github/workflows/release.yml` (em tag `v*`: mesmo build, anexa o `.exe` a uma GitHub Release; só roda quando o dono publicar o repositório)
- Modify: `Directory.Build.props` (`Version` 1.0.0), `README`/docs de instalação
- Test: `scripts/Test-Scripts.ps1` (analisa todos os scripts com o parser do **Windows PowerShell 5.1** e falha em erro; entra no CI)

- [ ] **Step 1:** teste que falha: `powershell.exe -NoProfile -File scripts/Test-Scripts.ps1` acusa o `#Requires -Version 7`.
- [ ] **Step 2:** adaptar os scripts; criar o `.iss` e o `Build-Release.ps1`.
- [ ] **Step 3:** **Inno Setup precisa estar instalado na máquina de build.** Pedir ao dono autorização para `winget install JRSoftware.InnoSetup` (download de software) antes de rodar.
- [ ] **Step 4:** `pwsh scripts/Build-Release.ps1` gera o `.exe`; `Test-Scripts.ps1` e o CI completo PASS.
- [ ] **Step 5:** commit `feat: ship Caixa-Preta as a single installer`.

---

### Task 7: Docs, revisão e instalação pelo `.exe`

- [ ] Docs: `docs/architecture/overview.md` (terceiro processo: app da bandeja e painel fixo), `flows.md` (painel fixo), `frontend.md` (exportação), `data-model.md` (sensores de disco), ADRs 0017 (identidade), 0018 (app da bandeja com WebView2 e painel fixo no lugar de notificações), 0019 (exportação no frontend), 0020 (instalador Inno Setup), AGENTS.md de cada projeto novo.
- [ ] Revisão final do branch por um revisor novo; corrigir o que for crítico ou importante com testes.
- [ ] O dono roda `CaixaPreta-Setup-1.0.0.exe` **por cima da instalação atual**. Verificar: serviços rodando, banco e histórico intactos, app na bandeja e painel fixo ao entrar no Windows, janela abrindo o painel, números do painel fixo batendo com o Ao vivo; exportar os três formatos; desinstalar e reinstalar mantendo os dados.
- [ ] Registrar o resultado neste plano e fazer o commit `docs: record plan 06 installation results`.

## Fora deste plano

- Assinatura de código do instalador (precisa de um certificado pago).
- Regras de diagnóstico para discos (por exemplo, queda logo depois de erro de disco); por enquanto o disco é só monitorado e exibido.
- Visão de meses no Histórico usando os resumos por hora.
- Notificações do Windows (o dono preferiu o painel fixo; voltam se ele pedir).
- Publicar o repositório no GitHub (o workflow de release fica pronto para quando o dono quiser).

## Critério de pronto

Todas as tasks com testes verdes; CI local verde; `CaixaPreta-Setup-1.0.0.exe` instalado por cima da versão atual nesta máquina sem perder dados; app na bandeja com o ícone da Caixa-Preta, painel fixo sempre visível, janela com o painel e exportação nos três formatos; revisão final feita por um revisor novo.
