# 0020 — Instalador único com Inno Setup

**Contexto.** A instalação era por scripts PowerShell 7 num terminal de administrador. O dono quer instalar por um `.exe` (uma release).

**Decisão.**
- `CaixaPreta-Setup-<versão>.exe`, feito com Inno Setup 6 (`installer/CaixaPreta.iss`), em português, pedindo administrador, pasta fixa `%ProgramFiles%\PcMonitor`.
- Os três executáveis vão *self-contained* (win-x64): a máquina não precisa do .NET. `scripts/Build-Release.ps1` publica, junta os scripts e compila; sai também o SHA-256.
- O `.iss` só copia arquivos e chama os scripts existentes com `-InPlace` (instalação) e `Uninstall-*` (remoção), pelo Windows PowerShell 5.1 que vem no Windows. Regra de instalação mora só nos scripts. `scripts/Test-Scripts.ps1` (no CI) garante que todos rodam no 5.1: sem sintaxe do 7, só ASCII, sem `$PSScriptRoot` em valor padrão de parâmetro.
- Atualizar: o instalador para os serviços e o app, apaga as pastas antigas dos componentes e roda os scripts; o banco não é tocado. Desinstalar pergunta se apaga `%ProgramData%\PcMonitor` (padrão: não).
- `.github/workflows/release.yml` gera o mesmo `.exe` numa tag `v*` e o anexa a uma GitHub Release.

**Alternativas.** MSI/WiX (mais cerimônia para um app de uma máquina); MSIX (não instala serviços Windows); manter só os scripts (o dono pediu um `.exe`).

**Consequências.** O instalador tem ~114 MB (três runtimes). Não é assinado (exige certificado pago), então o SmartScreen avisa na primeira execução. Instalar à mão pelos scripts e pelo `.exe` dá o mesmo resultado.
