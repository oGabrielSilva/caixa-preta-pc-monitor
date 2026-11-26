# Caixa-Preta

> 🇧🇷 Português · [🇺🇸 English](#english)

Monitor de hardware para Windows que registra temperaturas, tensões, consumo, carga, memória e discos de forma contínua e **à prova de queda de energia**, detecta cada desligamento inesperado e gera um relatório explicável sobre a causa provável (fonte, superaquecimento, placa-mãe ou software).

- Site e download: [ogabrielsilva.github.io/caixa-preta-pc-monitor](https://ogabrielsilva.github.io/caixa-preta-pc-monitor/)
- Estudo de arquitetura: [docs/estudos/2026-11-24-estudo-arquitetura.md](docs/estudos/2026-11-24-estudo-arquitetura.md)
- Licença: [MIT](LICENSE)

### Instalar (Windows 11, x64)

Execute `CaixaPreta-Setup-<versão>.exe` e aceite o pedido de administrador. O instalador traz tudo (não precisa de .NET) e:

- registra os serviços **Caixa-Preta (Coletor)** e **Caixa-Preta (Painel)** e instala o driver PawnIO via winget se faltar;
- instala o app da bandeja (`CaixaPreta.exe`), com o atalho "Caixa-Preta" no menu Iniciar; na primeira vez que abre ele passa a iniciar com o Windows (desligável no menu da bandeja);
- grava os dados em `%ProgramData%\PcMonitor\pcmonitor.db`. Atualizar mantém os dados; desinstalar pergunta se eles devem ser apagados.

### Gerar o instalador

Precisa de .NET 10, Node + pnpm (via corepack) e Inno Setup 6 (`winget install JRSoftware.InnoSetup`). Num terminal **sem** privilégio de administrador:

```powershell
powershell -File scripts/Build-Release.ps1
```

Sai em `artifacts/release/CaixaPreta-Setup-<versão>.exe`, com o `.sha256` ao lado. Os scripts `scripts/Install-*.ps1` e `Uninstall-*.ps1` também instalam cada parte a partir de um `dotnet publish` (veja o cabeçalho de cada um).

---

## English

Windows hardware monitor that continuously records temperatures, voltages, power, load, memory and disks in a **power-loss-safe** way, detects every unexpected shutdown and produces an explainable report on the likely cause (power supply, overheating, motherboard or software).

Install: run `CaixaPreta-Setup-<version>.exe` (self-contained, needs administrator). Build it with `powershell -File scripts/Build-Release.ps1` (.NET 10, pnpm and Inno Setup 6).

- Website and download: [ogabrielsilva.github.io/caixa-preta-pc-monitor/en/](https://ogabrielsilva.github.io/caixa-preta-pc-monitor/en/)
- Architecture study (Portuguese): [docs/estudos/2026-11-24-estudo-arquitetura.md](docs/estudos/2026-11-24-estudo-arquitetura.md)
- License: [MIT](LICENSE)
