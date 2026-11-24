# Fluxos

Status: ciclo do coletor ✅ (Plano 02) · detecção de incidentes ✅ (Plano 03) · diagnóstico ✅ (Plano 04).

## Ciclo de vida do coletor e amostragem ✅

```mermaid
flowchart TD
    A([Serviço inicia no boot]) --> B[Abrir banco + aplicar migrações]
    B --> C[Detectar incidentes<br/>ver abaixo]
    C --> D[Criar MonitoringSession]
    D --> E[Descobrir sensores e gravar catálogo do segmento]
    E --> F{{A cada 1 s}}
    F --> G[Ler sensores]
    G --> H[Gravar quadro<br/>commit + fsync]
    H --> F
    F -. evento do SCM .-> J{Qual?}
    J -- Shutdown / Preshutdown --> K[EndReason = SystemShutdown]
    J -- Stop --> L[EndReason = ServiceStopped]
    J -- Suspend --> S[Fechar segmento<br/>EndReason = Suspended]
    S -. Resume .-> E
    K --> M([Fechar sessão e banco])
    L --> M
```

- Além dos eventos do SCM, uma lacuna de mais de 3 × o intervalo sem evento de energia fecha o segmento com `Gap` (cobre o *Fast Startup*).
- `PcMonitorServiceLifetime` distingue *stop* de *shutdown* e repassa suspensão/retomada (`OnShutdown`, `OnPowerEvent`).
- **Watchdog:** a leitura roda na thread `PcMonitor.Sampler`. Sem ciclo concluído por 10 s, registra `SamplerStalled` (EventId 1001) e chama `Environment.FailFast`; as ações de recuperação do serviço o reiniciam.
- Decisões: [ADR 0011](../adr/0011-segments-gaps-and-watchdog.md).

## Detecção de incidentes ✅

O **Kernel-Power 41** do Log de Eventos é a fonte de verdade de que a máquina caiu ([ADR 0005](../adr/0005-event-log-as-incident-source-of-truth.md)).

```mermaid
flowchart TD
    A([Coletor inicia]) --> B[Ler eventos 41 ainda não importados<br/>inclusive anteriores à instalação]
    B --> C{Para cada evento 41}
    C --> D{Há segmento aberto com<br/>última amostra antes do boot?}
    D -- sim --> E[Incidente com telemetria<br/>120 s antes da última amostra]
    D -- não --> F[Incidente só com eventos]
    E --> G[Anexar 6008, 1074, 1001, WHEA próximos]
    F --> G
    G --> H[Diagnosticar e gravar]
    H --> C
    C -- fim --> I{Sessão sem EndReason<br/>e sem evento 41?}
    I -- sim --> J[EndReason = Abnormal<br/>falha do coletor, não do hardware]
    I -- não --> Z([Seguir para a amostragem])
    J --> Z
```

Detalhes da implementação (`DetectIncidents`, executado pelo coletor antes de abrir a sessão nova):

- **Todas as quedas do log são lidas a cada início**, na ordem de registro, e as já salvas são puladas antes da associação. Não há marca d'água por horário: o relógio do boot vem do RTC da placa, que uma placa com defeito pode errar.
- **Sessão da queda:** a sessão aberta mais recente cujo boot (medido pela própria sessão) é anterior ao 41 e não anterior ao boot da queda anterior.
- **Estimativa da queda:** último quadro (`LastFrame`); se a máquina estava entrando/saindo da suspensão, `LastFrameBeforeSleep` (limite inferior); se o Windows ainda estava vivo depois do último quadro, o coletor já tinha parado e vale o 6008 (`LastAliveEventLog`, também limite inferior); sem nada, `Unknown`.
- **6008 de cada queda:** o escrito no mesmo boot, pareado por ordem de registro (fica entre o 41 anterior e o 41 dele), nunca só por proximidade de horário.
- **Eventos relacionados:** de 10 min antes da estimativa (ou 40 min antes do 41, sem estimativa) até 2 min depois do 41, **recortados nas quedas vizinhas**: 6005/6006/6008, 1074/1076, BugCheck 1001, Kernel-Power 41/42/107/109 e WHEA.
- **Atomicidade:** o incidente e o fechamento da sessão que caiu são gravados na mesma transação. Uma queda que falha ao importar não impede as outras e é tentada de novo no próximo início.
- **Retenção:** os 120 s antes do fim da telemetria de cada incidente nunca são apagados, em todos os segmentos da sessão.
- Falha na detecção é registrada (EventId 1201) e a amostragem começa mesmo assim.

## Motor de diagnóstico ✅

Cada regra é uma classe que implementa `IDiagnosticRule` e devolve evidências para uma hipótese; o motor só agrega. Nova regra = nova classe registrada, sem alterar o motor.

```mermaid
flowchart LR
    I[Incidente<br/>janela pré-queda] --> R1[ThermalRule]
    I --> R2[RailOutOfToleranceRule]
    I --> R3[RailDroopTrendRule]
    I --> R4[LoadAtFailureRule]
    I --> R5[BugcheckPresenceRule]
    I --> R6[WheaPresenceRule]
    R1 & R2 & R3 & R4 & R5 & R6 --> E[DiagnosisEngine]
    E --> D[Diagnóstico<br/>ranking + evidências + limitações]
```

Executado pelo coletor ao iniciar, logo após a detecção (`StartupAnalysis`; cada etapa isolada). Regras, códigos, limiares e confiança: [diagnosis.md](diagnosis.md).
