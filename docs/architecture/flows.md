# Fluxos

Status: 🕓 planejado (Planos 02–04). Os diagramas descrevem o comportamento alvo.

## Ciclo de vida do coletor e amostragem (Plano 02)

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

- O `WindowsServiceLifetime` padrão não distingue *stop* de *shutdown*; o host terá um lifetime próprio que trata `OnShutdown`, *preshutdown* e `OnPowerEvent`.
- **Watchdog:** a leitura de sensores roda numa thread dedicada. Um ciclo acima de 10 s registra `SamplerStalled` e encerra o processo; as ações de recuperação do serviço o reiniciam.

## Detecção de incidentes (Plano 03)

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

## Motor de diagnóstico (Plano 04)

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

Tabela de regras, pesos e limitações: §6.3 do [estudo](../estudos/2026-11-24-estudo-arquitetura.md).
