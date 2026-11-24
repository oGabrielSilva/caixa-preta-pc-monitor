# PC Monitor

> 🇧🇷 Português · [🇺🇸 English](#english)

Monitor de hardware para Windows que registra temperaturas, tensões, consumo e carga de forma contínua e **à prova de queda de energia**, detecta cada desligamento inesperado e gera um relatório explicável sobre a causa provável (fonte, superaquecimento, placa-mãe ou software).

**Status:** em construção. O coletor já grava os sensores como serviço do Windows; detecção de incidentes, diagnóstico e painel ainda não estão prontos.

- Estudo de arquitetura: [docs/estudos/2026-11-24-estudo-arquitetura.md](docs/estudos/2026-11-24-estudo-arquitetura.md)
- Licença: [MIT](LICENSE)

### Instalar o coletor (Windows 11, .NET 10)

```powershell
dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector   # usuário comum
pwsh scripts/Install-Collector.ps1                                         # terminal de administrador
```

O script instala o driver PawnIO via winget se faltar, registra o serviço `PcMonitor.Collector` e grava em `%ProgramData%\PcMonitor\pcmonitor.db`. Para remover: `pwsh scripts/Uninstall-Collector.ps1` (mantém os dados; `-RemoveData` apaga).

---

## English

Windows hardware monitor that continuously records temperatures, voltages, power and load in a **power-loss-safe** way, detects every unexpected shutdown and produces an explainable report on the likely cause (power supply, overheating, motherboard or software).

**Status:** under construction. The collector already records sensors as a Windows service; incident detection, diagnosis and the dashboard are not ready yet.

- Architecture study (Portuguese): [docs/estudos/2026-11-24-estudo-arquitetura.md](docs/estudos/2026-11-24-estudo-arquitetura.md)
- License: [MIT](LICENSE)
