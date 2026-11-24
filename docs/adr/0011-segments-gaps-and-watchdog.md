# 0011 — Segmentos, lacunas, watchdog e sensores confiáveis

**Contexto.** O coletor roda dias seguidos e atravessa suspensões, hibernações (inclusive o *Fast Startup* do Windows, que hiberna os serviços em vez de pará-los), travamentos de leitura de hardware e execuções sem privilégio. Cada uma dessas situações, se ignorada, gera dados enganosos: leituras de dias diferentes "coladas", um serviço vivo que não grava nada ou zeros com cara de medição.

**Decisão.**
- **Segmentos:** uma sessão (uma execução do coletor) é dividida em segmentos contínuos. Um segmento termina com `Suspended` (evento de energia do Windows), com `Gap` (nenhum quadro por mais de 3 × o intervalo, sem evento de energia; o horário de fim é o do último quadro) ou junto com a sessão (`ServiceStopped`/`SystemShutdown`).
- **Shutdown × stop:** um `WindowsServiceLifetime` próprio (`PcMonitorServiceLifetime`) registra o shutdown do Windows e os eventos de energia, que o padrão do .NET trata como uma parada comum.
- **Watchdog:** a leitura roda numa thread dedicada. Se nenhum ciclo terminar em 10 s, o processo é encerrado com `Environment.FailFast`, e as ações de recuperação do serviço (reinício em 5 s, 5 s e 30 s) o reiniciam. Falha ao iniciar a amostragem segue o mesmo caminho.
- **Sensores confiáveis:** sensores que dependem do driver só são gravados quando o PawnIO está instalado **e** o processo tem privilégio de administrador. Fora disso a sessão fica `DriverMissing` e só a carga é gravada (a LibreHardwareMonitor devolve zeros ou valores fixos, sem erro).
- **Falhas de gravação** (disco cheio, banco ocupado) são registradas e o próximo ciclo tenta de novo; não derrubam o serviço.

**Alternativas.** Timeout por ciclo (não cancela uma leitura travada dentro do driver); confiar apenas em `PawnIo.IsInstalled` (gravaria zeros numa execução sem privilégio).

**Consequências.** O Plano 03 detecta quedas pelo Event Log e associa cada uma ao segmento cuja última amostra precede o boot. Publicação atual do coletor: dependente do runtime .NET 10 instalado; o empacotamento *self-contained* do [ADR 0006](0006-no-native-aot.md) fica para o plano do instalador.
