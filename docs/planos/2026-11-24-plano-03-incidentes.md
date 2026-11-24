# Plano 03 — Detecção de incidentes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** a cada início do coletor, importar do Log de Eventos do Windows todo desligamento inesperado (Kernel-Power 41) ainda não registrado, ligá-lo à telemetria gravada antes da queda quando existir, anexar os eventos relacionados, fechar as sessões órfãs e proteger da retenção os segundos que antecederam cada queda.

**Architecture:** o Domain ganha `SystemEvent`, `UnexpectedShutdown` e `Incident`. A Application ganha as portas `ISystemEventLog` e `IIncidentStore` e o caso de uso `DetectIncidents`, que contém toda a regra de associação. O adapter Windows lê o Log de Eventos (`EventLogReader` + XPath) e converte o XML com um parser puro. O SQLite ganha a migração 003. O coletor roda a detecção uma vez, antes de abrir a nova sessão.

**Tech Stack:** .NET 10 · `System.Diagnostics.Eventing.Reader` · Microsoft.Data.Sqlite + Dapper · xUnit v3, Shouldly.

**Spec:** [estudo](../estudos/2026-11-24-estudo-arquitetura.md) §6.2 · [ADR 0005](../adr/0005-event-log-as-incident-source-of-truth.md) · [ADR 0011](../adr/0011-segments-gaps-and-watchdog.md) · [modelo de dados](../architecture/data-model.md).

**Fora deste plano:** diagnóstico e regras (Plano 04), painel (Plano 05). Até lá, os incidentes são conferidos com o script de verificação.

## Fatos verificados nesta máquina (24/11/2026)

- 16 eventos Kernel-Power 41 (desde 16/11), cada um com um 6008 registrado segundos depois. Log do Sistema circular de 20 MB (cobre desde 29/10).
- **41:** `TimeCreated` ≈ boot **seguinte** à queda (≈ 2 s após o Kernel-General 12). `EventData` nomeado: `BugcheckCode`, `SleepInProgress`, `PowerButtonTimestamp` (FILETIME, 0 = não pressionado), `LongPowerButtonPressDetected`, `WHEABootErrorCount` etc.
- **6008:** dados posicionais com texto **localizado** e caracteres invisíveis (`‎24/‎11/‎2026`). O `<Binary>` contém duas `SYSTEMTIME` de 16 bytes: local e **UTC** (a segunda). Esse horário é o **último sinal de vida** registrado pelo Windows (pode ficar até ~20 min antes do boot seguinte), não o instante exato da queda.
- **1074** (User32): desligamento iniciado por usuário/processo.

## Global Constraints

- Tudo do Plano 02 continua valendo (camadas, CPM, testes antes, inglês no código, português nos docs, `net10.0-windows` no lado do coletor).
- Um incidente nasce **só** de um Kernel-Power 41 (ADR 0005). Chave de idempotência: `(event_record_id, logged_at_ms)`.
- Horário estimado da queda, em ordem: último quadro da sessão associada (`LastFrame`) → horário UTC do 6008 do mesmo boot (`LastAliveEventLog`) → desconhecido (`Unknown`).
- Janela de eventos relacionados: de `(estimativa ?? logged_at − 30 min) − 10 min` até `logged_at + 2 min`. Eventos: 6008 e 6005/6006 (EventLog), 1074/1076 (User32), 1001 (Microsoft-Windows-WER-SystemErrorReporting), 41/42/107/109 (Microsoft-Windows-Kernel-Power), todos do Microsoft-Windows-WHEA-Logger.
- Retenção: quadros dos **120 s** anteriores à estimativa de cada incidente com telemetria **nunca** são apagados.
- A detecção roda **antes** de `MonitoringRecorder.Start()`. Falha na detecção é registrada e **não** impede a amostragem.
- Novo `EndReason.UnexpectedShutdown`; sessões abertas sem 41 correspondente recebem `EndReason.Abnormal`. Horário de fim = último quadro (ou início da sessão, se não houver quadros).

## Review Focus

1. **Queda durante o boot, antes de o coletor subir** (dois 41 seguidos, uma única sessão aberta): a sessão fica com o 41 mais antigo posterior ao seu início; o outro vira incidente sem telemetria. Teste na Task 2.
2. **Queda durante a suspensão** (último segmento já fechado como `Suspended`, sessão aberta): o incidente usa esse último segmento como telemetria e `SleepInProgress` do 41 é preservado. Teste na Task 2.
3. **Coletor reiniciado várias vezes sem queda** (sessões fechadas normalmente): nenhum incidente, nenhuma sessão alterada. Teste na Task 2.
4. **Log de Eventos sem 6008 ou com binário truncado:** incidente com estimativa `Unknown`, sem exceção. Teste na Task 4.
5. **Detecção rodando de novo** (reinício do coletor): nenhum incidente duplicado. Testes nas Tasks 2 e 3.

---

### Task 1: Domínio de incidentes

**Files:**
- Create: `src/PcMonitor.Domain/Incidents/SystemEvent.cs`, `UnexpectedShutdown.cs`, `Incident.cs`, `FailureTimeSource.cs`
- Modify: `src/PcMonitor.Domain/Monitoring/EndReason.cs` (acrescentar `UnexpectedShutdown`)
- Test: `tests/PcMonitor.Domain.Tests/Incidents/UnexpectedShutdownTests.cs`

**Interfaces (Produces):**
- `sealed record SystemEvent(string Provider, int EventId, long RecordId, DateTimeOffset LoggedAt, int Level, IReadOnlyDictionary<string, string> Data)`
- `sealed record UnexpectedShutdown(long RecordId, DateTimeOffset LoggedAt, int BugcheckCode, bool SleepInProgress, DateTimeOffset? PowerButtonPressedAt, bool LongPowerButtonPress, DateTimeOffset? LastAliveAt, IReadOnlyDictionary<string, string> Data)` — `LastAliveAt` vem do 6008 do mesmo boot. Propriedade calculada `bool HadBugcheck => BugcheckCode != 0`.
- `enum FailureTimeSource { LastFrame, LastAliveEventLog, Unknown }`
- `sealed record Incident(UnexpectedShutdown Shutdown, DateTimeOffset? EstimatedFailureAt, FailureTimeSource FailureTimeSource, long? SessionId, long? SegmentId, IReadOnlyList<SystemEvent> RelatedEvents)`

- [ ] **Step 1: Teste que falha** — `HadBugcheck` é `false` para 0 e `true` para `0x9C`.
- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: add incident domain model`

---

### Task 2: Caso de uso `DetectIncidents`

**Files:**
- Create: `src/PcMonitor.Application/Incidents/ISystemEventLog.cs`, `IIncidentStore.cs`, `OpenSession.cs`, `DetectIncidents.cs`, `DetectionResult.cs`
- Test: `tests/PcMonitor.Application.Tests/Incidents/DetectIncidentsTests.cs`, `Fakes/FakeSystemEventLog.cs`, `Fakes/InMemoryIncidentStore.cs`

**Interfaces (Produces):**
- `interface ISystemEventLog { IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns(DateTimeOffset? after); IReadOnlyList<SystemEvent> ReadRelated(DateTimeOffset from, DateTimeOffset to); }` — `ReadUnexpectedShutdowns` devolve em ordem cronológica, só os com `LoggedAt > after`.
- `sealed record OpenSession(long SessionId, DateTimeOffset StartedAt, long? LastSegmentId, long? OpenSegmentId, DateTimeOffset? LastFrameAt)` — `LastSegmentId` é o último segmento da sessão, aberto ou não; `OpenSegmentId` só se ainda estiver aberto.
- `interface IIncidentStore`:
  - `DateTimeOffset? LatestIncidentLoggedAt()`
  - `IReadOnlyList<OpenSession> OpenSessions()` — sessões sem `end_reason`, por `started_at`.
  - `bool SaveIncident(Incident incident)` — `false` se a chave `(record_id, logged_at)` já existir.
  - `void EndSession(OpenSession session, DateTimeOffset at, EndReason reason)` — fecha o segmento aberto (se houver) e a sessão, na mesma transação.
- `sealed record DetectionResult(int Incidents, int WithTelemetry, int AbnormalSessions)`
- `sealed partial class DetectIncidents(ISystemEventLog eventLog, IIncidentStore store, ILogger<DetectIncidents> logger)` — `DetectionResult Run()`:
  1. `shutdowns = eventLog.ReadUnexpectedShutdowns(store.LatestIncidentLoggedAt())`; `open = store.OpenSessions()`.
  2. Para cada 41 em ordem: sessão = a **mais recente** de `open` com `StartedAt < LoggedAt` ainda não usada; estimativa e fonte pela regra das Global Constraints; `related = eventLog.ReadRelated(janela)`; `SaveIncident`; se houve sessão, `EndSession(sessão, LastFrameAt ?? StartedAt, UnexpectedShutdown)`; log Warning (EventId 200) com horário estimado e fonte.
  3. Sessões de `open` não usadas → `EndSession(…, Abnormal)`; log Warning (EventId 201).

- [ ] **Step 1: Testes que falham** (fakes à mão; horários fixos em 24/11/2026)

```csharp
[Fact] public void Crash_with_an_open_session_uses_the_last_frame_as_failure_time()
[Fact] public void Crash_without_a_session_uses_the_event_log_last_alive_time()
[Fact] public void Crash_without_session_or_last_alive_has_an_unknown_failure_time()
[Fact] public void Crashed_session_is_ended_as_UnexpectedShutdown_at_its_last_frame()
[Fact] public void Two_crashes_and_one_open_session_give_telemetry_only_to_the_first_crash_after_the_session_started() // Review Focus 1
[Fact] public void Crash_during_sleep_uses_the_last_closed_segment_and_keeps_SleepInProgress()                      // Review Focus 2
[Fact] public void Cleanly_closed_sessions_produce_no_incident_and_are_not_touched()                                  // Review Focus 3
[Fact] public void Open_session_without_a_crash_event_is_ended_as_Abnormal()
[Fact] public void Already_saved_crashes_are_not_saved_again()                                                        // Review Focus 5
[Fact] public void Related_events_are_read_from_ten_minutes_before_the_estimate_to_two_minutes_after_the_event()
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: detect incidents from unexpected shutdown events`

---

### Task 3: Persistência de incidentes e proteção na retenção

**Files:**
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/Migrations/003_incidents.sql`
- Create: `src/.../Persistence.Sqlite/Incidents/SqliteIncidentStore.cs`, `Incidents/IncidentSql.cs`
- Modify: `src/.../Persistence.Sqlite/Retention/RetentionSql.cs` (`DeleteFramesBatch` não apaga janelas de incidente)
- Test: `tests/.../SqliteIncidentStoreTests.cs`, acréscimos em `SqliteRetentionStoreTests.cs` e `SchemaMigratorTests.cs` (`LatestVersion = 3`)

**`003_incidents.sql`:**

```sql
CREATE TABLE incident (
    id INTEGER PRIMARY KEY,
    event_record_id INTEGER NOT NULL, logged_at_ms INTEGER NOT NULL,
    estimated_failure_ms INTEGER, failure_time_source TEXT NOT NULL,
    session_id INTEGER REFERENCES session (id), segment_id INTEGER REFERENCES segment (id),
    bugcheck_code INTEGER NOT NULL, sleep_in_progress INTEGER NOT NULL,
    power_button_ms INTEGER, long_power_button INTEGER NOT NULL, last_alive_ms INTEGER,
    data_json TEXT NOT NULL,
    UNIQUE (event_record_id, logged_at_ms));
CREATE INDEX ix_incident_segment ON incident (segment_id);
CREATE TABLE incident_event (
    incident_id INTEGER NOT NULL REFERENCES incident (id),
    provider TEXT NOT NULL, event_id INTEGER NOT NULL, record_id INTEGER NOT NULL,
    logged_at_ms INTEGER NOT NULL, level INTEGER NOT NULL, data_json TEXT NOT NULL,
    PRIMARY KEY (incident_id, provider, record_id)) WITHOUT ROWID;
```

**Retenção:** `DELETE … WHERE ts_ms < @CutoffMs AND NOT EXISTS (SELECT 1 FROM incident i WHERE i.segment_id = frame.segment_id AND frame.ts_ms BETWEEN i.estimated_failure_ms - @WindowMs AND i.estimated_failure_ms)`, `WindowMs = 120000`.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void SaveIncident_stores_the_incident_and_its_related_events()
[Fact] public void SaveIncident_returns_false_for_a_crash_already_saved()              // Review Focus 5
[Fact] public void LatestIncidentLoggedAt_is_the_newest_crash_or_null()
[Fact] public void OpenSessions_lists_sessions_without_end_reason_with_their_last_and_open_segments_and_last_frame()
[Fact] public void EndSession_closes_the_open_segment_and_the_session_with_time_and_reason()
[Fact] public void Retention_keeps_the_120_seconds_before_an_incident()
[Fact] public void Migrate_reaches_version_3()
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: persist incidents and protect pre-failure frames from retention`

---

### Task 4: Leitura do Log de Eventos do Windows

**Files:**
- Create: `src/PcMonitor.Infrastructure.Windows/EventLog/EventXml.cs` (parser puro: XML → `SystemEvent`), `LastAliveTime.cs` (binário do 6008 → `DateTimeOffset?` UTC), `UnexpectedShutdownMapper.cs` (`SystemEvent` 41 + 6008 → `UnexpectedShutdown`), `WindowsEventLogReader.cs` (`ISystemEventLog`)
- Create: `tests/PcMonitor.Infrastructure.Windows.Tests/` (novo, `net10.0-windows`, `AGENTS.md`/`CLAUDE.md`) com `Fixtures/kernel-power-41.xml`, `eventlog-6008.xml`, `user32-1074.xml` (copiados desta máquina, Computer anonimizado)
- Test: `EventXmlTests.cs`, `LastAliveTimeTests.cs`, `UnexpectedShutdownMapperTests.cs`, `WindowsEventLogReaderTests.cs`

**Interfaces (Produces):**
- `static class EventXml { SystemEvent Parse(string xml); }` — `Data` usa o atributo `Name`; sem nome, a chave é a posição (`"0"`, `"1"`…); o `<Binary>` vai para a chave `"Binary"`.
- `static class LastAliveTime { DateTimeOffset? FromBinary(string? hex); }` — segunda `SYSTEMTIME` (bytes 16–31, little-endian: ano, mês, dia da semana, dia, hora, minuto, segundo, ms) como UTC; `null` se ausente, curto ou inválido.
- `static class UnexpectedShutdownMapper { UnexpectedShutdown Map(SystemEvent kernelPower41, SystemEvent? eventLog6008); }` — `PowerButtonTimestamp` FILETIME ≠ 0 → `DateTimeOffset.FromFileTime`.
- `sealed class WindowsEventLogReader : ISystemEventLog` — consultas XPath no log `System`; o 6008 do mesmo boot é o primeiro com `LoggedAt` entre `41.LoggedAt − 2 min` e `41.LoggedAt + 2 min`.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Parse_reads_provider_id_record_time_level_and_named_data()        // fixture 41: BugcheckCode "0", RecordId 402399
[Fact] public void Parse_keys_unnamed_data_by_position_and_keeps_the_binary()          // fixture 6008
[Fact] public void LastAlive_is_the_UTC_systemtime_in_the_6008_binary()                // fixture → 2026-11-24T16:50:34.437Z
[Theory, InlineData(null), InlineData(""), InlineData("EA070B00")] public void LastAlive_is_null_for_missing_or_short_binary(string? hex) // Review Focus 4
[Fact] public void Mapper_builds_the_shutdown_from_41_and_its_6008()
[Fact] public void Mapper_without_6008_has_no_last_alive_time()                        // Review Focus 4
[Fact] public void Reader_reads_this_machines_system_log_without_throwing()             // integração, só leitura
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: read unexpected shutdowns and related events from the Windows event log`

---

### Task 5: Coletor executa a detecção; docs

**Files:**
- Modify: `src/PcMonitor.Collector/Hosting/CollectorServiceCollectionExtensions.cs` (registra `ISystemEventLog`, `IIncidentStore`, `DetectIncidents`)
- Create: `src/PcMonitor.Collector/Hosting/StartupIncidentDetection.cs` — executa `DetectIncidents.Run()` com try/catch e log (EventId 1201 em falha)
- Modify: `src/PcMonitor.Collector/Program.cs` — após `Migrate()` e antes de `RunAsync()`
- Modify: `docs/architecture/flows.md` (detecção ✅), `data-model.md` (tabelas de incidente ✅), AGENTS de Windows/Persistence/Collector
- Test: `tests/PcMonitor.Collector.Tests/Hosting/StartupIncidentDetectionTests.cs`

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Startup_detection_logs_and_continues_when_detection_throws()
[Fact] public void Collector_registers_incident_detection_services()   // ServiceDescriptors contêm ISystemEventLog, IIncidentStore, DetectIncidents
```

- [ ] **Step 2–4:** falhar → implementar → passar; suíte completa, format, agent docs, jscpd.
- [ ] **Step 5: Commit** — `feat: detect incidents when the collector starts`

---

### Task 6: Reinstalar e verificar nesta máquina

- [ ] **Step 1:** `dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector`; atualizar o script de verificação (scratchpad) para listar incidentes.
- [ ] **Step 2:** o **usuário** reinstala como administrador (`Install-Collector.ps1`) e roda a verificação.
- [ ] **Step 3: Esperado:** `user_version = 3`; **16 incidentes** (os 41 existentes), todos `LastAliveEventLog`, sem sessão, `bugcheck_code = 0`; sessões 1–3 intactas (`ServiceStopped`); a sessão nova aberta; Log de Eventos com um Warning 200 por incidente na primeira execução e nenhum na seguinte.
- [ ] **Step 4:** registrar o resultado neste plano e commitar.

## Critério de pronto

- Suítes verdes; incidentes existentes importados; reiniciar o coletor não duplica nada.
- A próxima queda real da máquina gera um incidente com `LastFrame` e os 120 s anteriores preservados (verificado quando acontecer).
