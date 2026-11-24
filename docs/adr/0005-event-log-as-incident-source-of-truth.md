# 0005 — Log de Eventos como fonte de verdade dos incidentes

**Contexto.** Uma sessão do coletor sem encerramento limpo não prova que a máquina caiu: o próprio coletor pode ter morrido ou ter sido encerrado pelo Windows. Quedas antes da instalação ou durante o boot não têm sessão.

**Decisão.** Um incidente nasce de um evento **Kernel-Power 41**. A telemetria do segmento anterior é anexada quando existir. Eventos 6008, 1074, BugCheck 1001 e WHEA próximos são correlacionados. Sessão aberta sem evento 41 é registrada como falha do coletor, não do hardware.

**Alternativas.** Heartbeat da sessão como fonte (falsos positivos).

**Consequências.** As quedas anteriores à instalação aparecem no painel desde o primeiro dia, sem telemetria. A ausência de bugcheck é tratada como evidência fraca, não como prova de corte de energia.
