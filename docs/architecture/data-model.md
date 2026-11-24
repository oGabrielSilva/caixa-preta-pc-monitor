# Modelo de dados

Decisões em [ADR 0004](../adr/0004-sqlite-wal-full-sync-frame-rows.md) e [ADR 0011](../adr/0011-segments-gaps-and-watchdog.md). Schema versionado por `PRAGMA user_version`, scripts em `src/PcMonitor.Infrastructure.Persistence.Sqlite/Migrations/`.

## Implementado ✅ (migrações 001–003)

```mermaid
erDiagram
    SESSION ||--o{ SEGMENT : contem
    SEGMENT ||--o{ SEGMENT_SENSOR : define_ordem
    SENSOR ||--o{ SEGMENT_SENSOR : aparece_em
    SEGMENT ||--o{ FRAME : contem
    SEGMENT ||--o{ AGGREGATE_1M : resume
    SENSOR ||--o{ AGGREGATE_1M : resume
    SESSION { int id  int started_at_ms  int booted_at_ms  text source_status  text board_product  int ended_at_ms  text end_reason }
    SEGMENT { int id  int session_id  int started_at_ms  int last_frame_ms  int ended_at_ms  text end_reason }
    SENSOR { int id  text stable_key }
    SEGMENT_SENSOR { int segment_id  int ordinal  int sensor_id  text name  text kind  text unit  text role }
    FRAME { int id  int segment_id  int ts_ms  blob values }
    AGGREGATE_1M { int segment_id  int minute_ms  int sensor_id  real min  real avg  real max }
```

### Incidentes ✅ (migração 003)

```mermaid
erDiagram
    SESSION ||--o| INCIDENT : caiu_em
    SEGMENT ||--o| INCIDENT : telemetria
    INCIDENT ||--o{ INCIDENT_EVENT : correlaciona
    INCIDENT { int id  int event_record_id  int logged_at_ms  int estimated_failure_ms  text failure_time_source  int session_id  int segment_id  int bugcheck_code  int sleep_in_progress  int power_button_ms  int long_power_button  int last_alive_ms  text data_json }
    INCIDENT_EVENT { int incident_id  text provider  int event_id  int record_id  int logged_at_ms  int level  text data_json }
```

## Planejado 🕓 (Plano 04)

```mermaid
erDiagram
    INCIDENT ||--o| DIAGNOSIS : tem
    DIAGNOSIS { int incident_id  text ranking_json  text evidence_json }
```

| Ponto | Decisão |
|---|---|
| Uma linha por instante | `frame.values` é um vetor `float32` little-endian na ordem de `segment_sensor.ordinal`; só o `FrameCodec` conhece o formato. `NaN` = sem valor. |
| Chave do quadro | `rowid`; índice **não único** em `(segment_id, ts_ms)`: quadros com horário repetido ou fora de ordem (ajuste de relógio) são todos mantidos. |
| Sensores | `sensor` guarda só a identidade (`stable_key`, o identificador da LibreHardwareMonitor). Nome, tipo, unidade e papel ficam em `segment_sensor`, **por segmento**: corrigir um perfil de placa depois nunca reescreve o significado do histórico. Os valores gravados já estão multiplicados pelo perfil. |
| Durabilidade | WAL + `synchronous=FULL` + WAL persistente (o painel lê mesmo sem permissão de escrita na pasta). |
| Retenção | Quadros por 14 dias, **exceto os 120 s antes de cada incidente com telemetria**; agregados de 1 min (mín/méd/máx, ignorando `NaN`) para sempre; job a cada 1 h, em transações curtas. |
| Incidentes | Um por Kernel-Power 41, idempotente por `(event_record_id, logged_at_ms)`; `failure_time_source` diz de onde vem a estimativa. |
| Local | `%ProgramData%\PcMonitor\pcmonitor.db`; ACL: SYSTEM e Administradores. |
| Volume esperado | ~15–20 MB/dia no banco; < 1 GB/dia de escrita física. |
