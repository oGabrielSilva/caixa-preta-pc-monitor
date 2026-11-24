# Modelo de dados

Decisões em [ADR 0004](../adr/0004-sqlite-wal-full-sync-frame-rows.md) e [ADR 0011](../adr/0011-segments-gaps-and-watchdog.md). Schema versionado por `PRAGMA user_version`, scripts em `src/PcMonitor.Infrastructure.Persistence.Sqlite/Migrations/`.

## Implementado ✅ (migração 001)

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
    SENSOR { int id  text stable_key  text name  text kind  text unit  text role }
    SEGMENT_SENSOR { int segment_id  int ordinal  int sensor_id }
    FRAME { int id  int segment_id  int ts_ms  blob values }
    AGGREGATE_1M { int segment_id  int minute_ms  int sensor_id  real min  real avg  real max }
```

## Planejado 🕓 (Plano 03)

```mermaid
erDiagram
    INCIDENT }o--o| SEGMENT : telemetria_opcional
    INCIDENT ||--o{ SYSTEM_EVENT : correlaciona
    INCIDENT ||--o| DIAGNOSIS : tem
    INCIDENT { int id  int segment_id_nullable  int event_record_id  int boot_ms  int estimated_failure_ms }
    SYSTEM_EVENT { int incident_id  int event_id  text provider  int ts_ms  text payload_json }
    DIAGNOSIS { int incident_id  text ranking_json  text evidence_json }
```

| Ponto | Decisão |
|---|---|
| Uma linha por instante | `frame.values` é um vetor `float32` little-endian na ordem de `segment_sensor.ordinal`; só o `FrameCodec` conhece o formato. `NaN` = sem valor. |
| Chave do quadro | `rowid`; índice **não único** em `(segment_id, ts_ms)`: quadros com horário repetido ou fora de ordem (ajuste de relógio) são todos mantidos. |
| Sensores | `sensor.stable_key` é o identificador da LibreHardwareMonitor; nome, papel e multiplicador vêm do perfil da placa (`Sensors.Lhm/Profiles/*.json`). |
| Durabilidade | WAL + `synchronous=FULL` + WAL persistente (o painel lê mesmo sem permissão de escrita na pasta). |
| Retenção | Quadros por 14 dias; agregados de 1 min (mín/méd/máx, ignorando `NaN`) para sempre; job a cada 1 h, em transações curtas. |
| Local | `%ProgramData%\PcMonitor\pcmonitor.db`; ACL: SYSTEM e Administradores. |
| Volume esperado | ~15–20 MB/dia no banco; < 1 GB/dia de escrita física. |
