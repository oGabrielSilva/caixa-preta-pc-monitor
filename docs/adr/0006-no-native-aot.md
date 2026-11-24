# 0006 — Sem Native AOT

**Contexto.** Native AOT geraria executáveis menores e de inicialização mais rápida.

**Decisão.** Não usar Native AOT nem trimming. Publicação *self-contained single-file*.

**Alternativas.** Native AOT: incompatível com a LibreHardwareMonitorLib, que depende de `System.Management` (WMI) e de reflexão.

**Consequências.** Executáveis maiores (dezenas de MB) e ~30–50 MB de memória por processo, aceitáveis para um serviço local.
