# installer

O instalador do Caixa-Preta (Inno Setup 6, ADR 0020): `CaixaPreta-Setup-<versão>.exe`, um arquivo só, em português.

- Não rode `ISCC.exe` direto: `powershell -File scripts/Build-Release.ps1` publica os três executáveis
  (*self-contained*, win-x64), junta os scripts e passa `/DAppVersion` e `/DStaging`.
- O `.iss` só copia os arquivos e chama os scripts `Install-*.ps1 -InPlace` / `Uninstall-*.ps1` com o Windows
  PowerShell 5.1. Regra de instalação (serviços, ACL, atalho) mora nos scripts, nunca aqui.
- A pasta é fixa (`%ProgramFiles%\PcMonitor`): os scripts e o app da bandeja dependem dela.
- Os scripts precisam passar em `powershell.exe -NoProfile -File scripts/Test-Scripts.ps1` (só ASCII, sem sintaxe do 7).
- O `.iss` é UTF-8 com BOM (textos em português).
