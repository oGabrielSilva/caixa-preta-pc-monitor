# 0010 — PawnIO instalado via winget e verificado pelo coletor

**Contexto.** A LibreHardwareMonitorLib lê o chip da placa e o processador pelo driver PawnIO ([ADR 0002](0002-librehardwaremonitor-pawnio.md)), mas o pacote NuGet não traz o instalador do driver. No spike de 24/11/2026, sem o driver, a biblioteca devolveu temperatura 0 e tensões fixas, sem nenhum erro: dados falsos com cara de leitura.

**Decisão.**
- O instalador do PC Monitor instala o driver com `winget install --id namazso.PawnIO -e` quando o serviço `PawnIO` não existir. Não redistribuímos o binário do PawnIO.
- O coletor verifica, ao iniciar, se o driver está carregado e se a placa foi detectada. Se não estiver, registra o estado **"driver ausente"** e não grava leituras de sensores que dependem do driver. O painel mostra um aviso com a instrução de instalação.

**Alternativas.** Levar o `PawnIO_setup.exe` oficial dentro do pacote (GPL-2.0 permite, com licença e link para o código-fonte; funciona offline, mas exige atualizar o binário a cada versão do driver).

**Consequências.** A instalação precisa de internet e do winget (presente no Windows 11). A detecção de "driver ausente" é obrigatória mesmo assim, porque o driver pode ser removido depois da instalação. Entregas: verificação no Plano 02 (coletor), instalação no plano do instalador.
