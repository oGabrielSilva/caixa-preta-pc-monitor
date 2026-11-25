# 0016 — Retenção dos agregados: 90 dias por minuto, por hora para sempre

**Contexto.** Os quadros brutos já tinham limite (14 dias, [ADR 0004](0004-sqlite-wal-full-sync-frame-rows.md)), mas os agregados de 1 minuto eram guardados para sempre: com 35 sensores, ~3,3 MB/dia, ~1,2 GB por ano, enquanto o Histórico mostra no máximo 14 dias. O WAL persistente também não tinha teto. O dono não quer um banco de gigas sem necessidade.

**Decisão.**
- Agregados de 1 minuto por **90 dias** (`Collector:AggregateRetention`, nunca menor que `RawRetention`).
- **Resumo por hora e por sensor** (`aggregate_1h`: mín. dos mínimos, média das médias, máx. dos máximos) **para sempre**, independente dos segmentos.
- Ordem da retenção: resumir minutos, resumir horas completas, apagar quadros antigos, apagar minutos antigos. Um minuto só é apagado se a hora dele já tem resumo.
- As consultas de resumo e de exclusão andam pelo índice `(sensor_id, minute_ms)`, uma por sensor, em transações curtas.
- WAL limitado a 64 MiB (`PRAGMA journal_size_limit` no escritor).

**Alternativas.** Só 90 dias, sem resumo por hora (perde tudo depois de 90 dias); manter tudo (cresce sem fim); resumo diário (pouco detalhe para ver tendências de semanas).

**Consequências.** Banco estável perto de 600 MB, crescendo ~20 MB por ano. Os resumos por hora ainda não aparecem no Histórico (os períodos vão até 14 dias); eles ficam prontos para uma visão de meses quando for útil.
