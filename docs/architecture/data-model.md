# Modelo de dados

Status: 🕓 planejado (Plano 02). Decisões em [ADR 0004](../adr/0004-sqlite-wal-full-sync-frame-rows.md).

```mermaid
erDiagram
    SESSION ||--o{ SEGMENT : contem
    SEGMENT ||--o{ SEGMENT_SENSOR : define_ordem
    SENSOR ||--o{ SEGMENT_SENSOR : aparece_em
    SEGMENT ||--o{ FRAME : contem
    SEGMENT ||--o{ AGGREGATE_1M : resume
    INCIDENT }o--o| SEGMENT : telemetria_opcional
    INCIDENT ||--o{ SYSTEM_EVENT : correlaciona
    INCIDENT ||--o| DIAGNOSIS : tem
    SESSION { int id  int started_at_ms  int ended_at_ms  text end_reason }
    SEGMENT { int id  int session_id  int started_at_ms  int last_frame_ms  text end_reason }
    SENSOR { int id  text stable_key  text name  text kind  text unit  text role }
    SEGMENT_SENSOR { int segment_id  int ordinal  int sensor_id }
    FRAME { int segment_id  int ts_ms  blob values }
    AGGREGATE_1M { int segment_id  int minute_ms  int sensor_id  real min  real avg  real max }
    INCIDENT { int id  int segment_id_nullable  int event_record_id  int boot_ms  int estimated_failure_ms }
    SYSTEM_EVENT { int incident_id  int event_id  text provider  int ts_ms  text payload_json }
    DIAGNOSIS { int incident_id  text ranking_json  text evidence_json }
```

| Ponto | Decisão |
|---|---|
| Uma linha por instante | `FRAME.values` é um vetor `float32` na ordem de `SEGMENT_SENSOR.ordinal`; só o `FrameCodec` conhece o formato |
| Durabilidade | WAL + `synchronous=FULL`; WAL persistente para o painel ler em modo somente leitura |
| Retenção | Quadros por 14 dias; agregados de 1 min para sempre; janelas de incidente nunca apagadas |
| Local | `C:\ProgramData\PcMonitor\pcmonitor.db` |
| Volume esperado | ~15–20 MB/dia no banco; < 1 GB/dia de escrita física |
