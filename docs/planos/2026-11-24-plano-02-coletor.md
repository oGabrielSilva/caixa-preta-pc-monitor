# Plano 02 — Coletor e persistência

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** o serviço `PcMonitor.Collector` lê os sensores 1×/s e grava cada leitura no SQLite de forma durável, separando sessões e segmentos, detectando driver ausente, sobrevivendo a leituras travadas e mantendo o banco pequeno; instalado e rodando nesta máquina.

**Architecture:** o Domain ganha o modelo de sensores e de sessões. A Application ganha portas (`ISensorSource`, `IMonitoringStore`, `IRetentionStore`) e casos de uso. O adapter LHM resolve papéis e multiplicadores por **perfil de placa** (JSON embutido), e o adapter SQLite grava **um quadro por segundo** (BLOB `float32`). O host executa a amostragem numa thread dedicada com watchdog e traduz eventos do Windows (shutdown, suspensão) em motivos de encerramento.

**Tech Stack:** .NET 10 · LibreHardwareMonitorLib 0.9.6 + PawnIO · Microsoft.Data.Sqlite 10.0.15 + Dapper · `Microsoft.Extensions.Hosting.WindowsServices` · xUnit v3, Shouldly, NSubstitute (entra agora, para fakes de `ISensor`/`IHardware` da LHM) · `Microsoft.Extensions.TimeProvider.Testing`.

**Spec:** [estudo de arquitetura](../estudos/2026-11-24-estudo-arquitetura.md) (§5–§7, §10) · [resultado do spike](../estudos/2026-11-24-resultado-spike-sensores.md) · ADRs [0003](../adr/0003-collector-dashboard-split-least-privilege.md), [0004](../adr/0004-sqlite-wal-full-sync-frame-rows.md), [0010](../adr/0010-pawnio-via-winget.md).

**Fora deste plano:** incidentes e Event Log (Plano 03, que também protege as janelas de incidente da retenção — até lá, quadros com mais de 14 dias são apagados, então o Plano 03 deve chegar antes disso para não perder a telemetria de quedas antigas), diagnóstico e tolerâncias ATX (Plano 04), leitura pelo painel, serviço do Dashboard e ACL da conta do painel (Plano 05).

## Global Constraints

- Banco: `%ProgramData%\PcMonitor\pcmonitor.db` (configurável em `Collector:DatabasePath`).
- `journal_mode=WAL`, `synchronous=FULL`, `foreign_keys=ON`, `busy_timeout=5000`, **WAL persistente** (`SQLITE_FCNTL_PERSIST_WAL`).
- Intervalo de amostragem: **1 s** (`Collector:SampleInterval`). Watchdog: ciclo acima de **10 s** encerra o processo (`Collector:StallTimeout`).
- Retenção: quadros brutos **14 dias** (`Collector:RawRetention`); agregados de 1 min (mín/méd/máx) sem prazo; job **a cada 1 h**.
- Lacuna sem evento de energia maior que **3 × intervalo** fecha o segmento com `EndReason.Gap` e abre outro.
- Timestamps em UTC, persistidos como **milissegundos Unix** (`INTEGER`).
- Valor de sensor ausente = `NaN` no quadro.
- Sem driver PawnIO: só sensores de **carga** (`Load`) são gravados; a sessão guarda o status `DriverMissing`.
- Perfis de placa: `src/PcMonitor.Infrastructure.Sensors.Lhm/Profiles/*.json`, embutidos. Perfil inicial: `asus-tuf-gaming-a620m-plus-wifi.json` com o mapeamento do spike.
- Tudo o que vale para o Plano 01 continua valendo: camadas, CPM, warnings como erro, inglês no código, português nos docs, testes antes do código.

## Review Focus

1. **Relógio voltando** (NTP, ajuste manual): dois quadros com o mesmo `ts_ms` ou fora de ordem são **ambos gravados**, sem erro de chave. Teste na Task 4.
2. **Sensor sem valor** (`null` momentâneo): o quadro é gravado com `NaN` nessa posição e os demais valores ficam intactos. Teste na Task 4.
3. **Catálogo de sensores diferente entre boots** (BIOS atualizada, módulo de RAM trocado): cada segmento decodifica com o próprio catálogo. Teste na Task 4.
4. **Falha ao gravar** (disco cheio, banco bloqueado): o ciclo registra o erro e o próximo ciclo tenta de novo; o serviço não cai em loop. Teste na Task 7.
5. **Fast Startup ou hibernação sem evento de energia:** a volta após uma lacuna abre um novo segmento, em vez de "colar" leituras de dias diferentes. Teste na Task 7.

---

### Task 1: Modelo de domínio de sensores e sessões

**Files:**
- Create: `src/PcMonitor.Domain/Sensors/SensorKind.cs`, `SensorRole.cs`, `SensorDescriptor.cs`, `SensorCatalog.cs`
- Create: `src/PcMonitor.Domain/Monitoring/EndReason.cs`, `SensorSourceStatus.cs`, `Frame.cs`
- Modify: `tests/PcMonitor.Domain.Tests/PcMonitor.Domain.Tests.csproj` (remover `--ignore-exit-code 8`)
- Test: `tests/PcMonitor.Domain.Tests/Sensors/SensorCatalogTests.cs`, `Monitoring/FrameTests.cs`

**Interfaces (Produces):**
- `enum SensorKind { Temperature, Voltage, Power, Load, Fan }`
- `enum SensorRole { None, CpuTemperature, CpuPackagePower, CpuLoad, Vcore, Rail12V, Rail5V, Rail3V3 }`
- `sealed record SensorDescriptor(string StableKey, string Name, SensorKind Kind, string Unit, SensorRole Role)`
- `sealed class SensorCatalog` — lista ordenada e imutável de `SensorDescriptor`; `int Count`; `SensorDescriptor this[int ordinal]`; `IReadOnlyList<SensorDescriptor> Sensors`; `int? OrdinalOf(SensorRole role)`. Construtor rejeita `StableKey` duplicada (`ArgumentException`) e papel ≠ `None` repetido.
- `enum EndReason { ServiceStopped, SystemShutdown, Suspended, Gap, Abnormal }` (`Abnormal` só é atribuído no Plano 03)
- `enum SensorSourceStatus { Ok, DriverMissing, BoardNotDetected }`
- `sealed record Frame(DateTimeOffset Timestamp, IReadOnlyList<float> Values)` — `Values.Count` precisa bater com o catálogo de quem grava; `NaN` = sem valor.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Catalog_keeps_the_given_order() { /* 3 descritores → Sensors.Select(StableKey) na mesma ordem; this[1] é o segundo */ }
[Fact] public void Catalog_rejects_duplicate_stable_keys() =>
    Should.Throw<ArgumentException>(() => new SensorCatalog([Sensor("/a"), Sensor("/a")]));
[Fact] public void Catalog_rejects_a_role_assigned_twice() { /* dois com Role = Rail12V → ArgumentException */ }
[Fact] public void OrdinalOf_finds_the_sensor_with_the_role() { /* Rail12V no índice 2 → 2; CpuTemperature ausente → null */ }
```

- [ ] **Step 2: Rodar e ver falhar** — `dotnet test --project tests/PcMonitor.Domain.Tests` → não compila.
- [ ] **Step 3: Implementar** os tipos (Domain continua sem dependências).
- [ ] **Step 4: Rodar e ver passar**; também `dotnet test --project tests/PcMonitor.Architecture.Tests`.
- [ ] **Step 5: Commit** — `feat: add sensor and monitoring domain model`

---

### Task 2: Portas e casos de uso de monitoramento

**Files:**
- Create: `src/PcMonitor.Application/Monitoring/ISensorSource.cs`, `SensorSourceInfo.cs`, `IMonitoringStore.cs`, `MonitoringRecorder.cs`, `SegmentHandle.cs`
- Create: `src/PcMonitor.Application/Retention/IRetentionStore.cs`, `RetentionPolicy.cs`, `ApplyRetention.cs`
- Modify: `tests/PcMonitor.Application.Tests/...csproj` (remover `--ignore-exit-code 8`; `Microsoft.Extensions.TimeProvider.Testing`)
- Test: `tests/PcMonitor.Application.Tests/Monitoring/MonitoringRecorderTests.cs`, `Retention/ApplyRetentionTests.cs`, `Fakes/InMemoryMonitoringStore.cs`, `Fakes/ScriptedSensorSource.cs`

**Interfaces (Produces):**
- `sealed record SensorSourceInfo(SensorSourceStatus Status, string? BoardProduct, SensorCatalog Catalog)`
- `interface ISensorSource { SensorSourceInfo Open(); IReadOnlyList<float> Read(); }` — `Read` devolve valores na ordem do catálogo de `Open`, `NaN` quando sem valor.
- `interface IMonitoringStore`:
  - `long StartSession(DateTimeOffset startedAt, DateTimeOffset bootedAt, SensorSourceInfo source)`
  - `long StartSegment(long sessionId, DateTimeOffset startedAt, SensorCatalog catalog)`
  - `void AppendFrame(long segmentId, Frame frame)`
  - `void EndSegment(long segmentId, DateTimeOffset endedAt, EndReason reason)`
  - `void EndSession(long sessionId, DateTimeOffset endedAt, EndReason reason)`
- `sealed record SegmentHandle(long SessionId, long SegmentId, DateTimeOffset LastFrameAt)`
- `sealed class MonitoringRecorder(ISensorSource source, IMonitoringStore store, TimeProvider time, ILogger<MonitoringRecorder> logger)` — **dono do estado da sessão**; os outros só chamam:
  - `SensorSourceInfo Start()` — abre a fonte, cria sessão e primeiro segmento (`bootedAt` = agora − `Environment.TickCount64`, via `Func<TimeSpan> uptime` injetável no construtor secundário de testes).
  - `bool RecordOnce()` — lê, grava um `Frame`; devolve `false` se a gravação falhou (loga, não lança).
  - `void BreakSegment(EndReason reason)` — fecha o segmento atual com o motivo e marca que o próximo `RecordOnce` abre um novo.
  - `void Stop(EndReason reason)` — fecha segmento e sessão.
  - Regra de lacuna: se `agora − último quadro > gapThreshold` (parâmetro do construtor, `TimeSpan`), `RecordOnce` fecha o segmento com `Gap` (horário = último quadro) e abre outro antes de gravar.
- `sealed record RetentionPolicy(TimeSpan RawRetention)`
- `interface IRetentionStore { int AggregateMinutesBefore(DateTimeOffset cutoff); int DeleteFramesBefore(DateTimeOffset cutoff); }`
- `sealed class ApplyRetention(IRetentionStore store, RetentionPolicy policy, TimeProvider time)` — `RetentionResult Run()`: agrega todos os minutos **completos** até agora, depois apaga quadros mais antigos que `agora − RawRetention`. `sealed record RetentionResult(int MinutesAggregated, int FramesDeleted)`.

`ILogger<T>` vem de `Microsoft.Extensions.Logging.Abstractions`: adicionar o pacote à Application e atualizar o `AGENTS.md` dela ("pode depender de: Domain, `TimeProvider`, abstrações de logging"). O Domain continua sem ele. `RecordOnce` captura exceções tanto da leitura quanto da gravação.

- [ ] **Step 1: Testes que falham** (fakes escritos à mão, sem NSubstitute)

```csharp
[Fact] public void Start_creates_a_session_and_a_segment_with_the_source_catalog()
[Fact] public void RecordOnce_appends_the_values_read_with_the_current_time()
[Fact] public void RecordOnce_returns_false_and_keeps_running_when_the_store_throws()      // Review Focus 4
[Fact] public void RecordOnce_after_BreakSegment_opens_a_new_segment()
[Fact] public void RecordOnce_after_a_gap_closes_the_segment_as_Gap_at_the_last_frame_time() // Review Focus 5
[Fact] public void Stop_ends_segment_and_session_with_the_reason()
[Fact] public void Retention_aggregates_complete_minutes_then_deletes_frames_older_than_14_days()
```

Valores fixos: `FakeTimeProvider` em `2026-11-24T12:00:00Z`; lacuna = 3 s; avançar 5 s entre leituras para o teste de lacuna; esperado `EndSegment(…, 12:00:00Z, EndReason.Gap)`.

- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar.**
- [ ] **Step 4: Rodar e ver passar** (Application + Architecture).
- [ ] **Step 5: Commit** — `feat: add monitoring recorder and retention use cases`

---

### Task 3: Conexão SQLite durável, WAL persistente e migrações

**Files:**
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/SqliteDatabase.cs` (abre conexões com os PRAGMAs; `OpenWriter()`, `OpenReader()`)
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/Native/PersistentWal.cs` (`[LibraryImport("e_sqlite3")] sqlite3_file_control`, op `10` = `SQLITE_FCNTL_PERSIST_WAL`)
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/Migrations/SchemaMigrator.cs`, `Migrations/001_monitoring.sql` (embedded resource)
- Modify: `tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests/...csproj` (remover `--ignore-exit-code 8`)
- Test: `tests/.../SqliteDatabaseTests.cs`, `SchemaMigratorTests.cs`, `Support/TempDatabase.cs`

**`001_monitoring.sql`:**

```sql
CREATE TABLE session (id INTEGER PRIMARY KEY, started_at_ms INTEGER NOT NULL, booted_at_ms INTEGER NOT NULL,
  source_status TEXT NOT NULL, board_product TEXT, ended_at_ms INTEGER, end_reason TEXT);
CREATE TABLE segment (id INTEGER PRIMARY KEY, session_id INTEGER NOT NULL REFERENCES session(id),
  started_at_ms INTEGER NOT NULL, last_frame_ms INTEGER, ended_at_ms INTEGER, end_reason TEXT);
CREATE TABLE sensor (id INTEGER PRIMARY KEY, stable_key TEXT NOT NULL UNIQUE, name TEXT NOT NULL,
  kind TEXT NOT NULL, unit TEXT NOT NULL, role TEXT NOT NULL);
CREATE TABLE segment_sensor (segment_id INTEGER NOT NULL REFERENCES segment(id), ordinal INTEGER NOT NULL,
  sensor_id INTEGER NOT NULL REFERENCES sensor(id), PRIMARY KEY (segment_id, ordinal)) WITHOUT ROWID;
CREATE TABLE frame (id INTEGER PRIMARY KEY, segment_id INTEGER NOT NULL REFERENCES segment(id),
  ts_ms INTEGER NOT NULL, "values" BLOB NOT NULL);
CREATE INDEX ix_frame_segment_ts ON frame (segment_id, ts_ms);
CREATE INDEX ix_frame_ts ON frame (ts_ms);
CREATE TABLE aggregate_1m (segment_id INTEGER NOT NULL REFERENCES segment(id), minute_ms INTEGER NOT NULL,
  sensor_id INTEGER NOT NULL REFERENCES sensor(id), min REAL, avg REAL, max REAL,
  PRIMARY KEY (segment_id, minute_ms, sensor_id)) WITHOUT ROWID;
```

`frame` usa `rowid` como chave e um índice **não único** em `(segment_id, ts_ms)` (Review Focus 1).

**Interfaces (Produces):**
- `sealed class SqliteDatabase(string path)` — `SqliteConnection OpenWriter()` (cria a pasta, aplica `journal_mode=WAL`, `synchronous=FULL`, `foreign_keys=ON`, `busy_timeout=5000` e WAL persistente); `SqliteConnection OpenReader()` (`Mode=ReadOnly`, `busy_timeout=5000`).
- `sealed class SchemaMigrator(SqliteDatabase db)` — `int Migrate()` aplica scripts embutidos `NNN_*.sql` em ordem, cada um numa transação, e grava `PRAGMA user_version`; devolve a versão final.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Writer_uses_wal_and_full_synchronous()            // PRAGMA journal_mode → "wal"; synchronous → 2
[Fact] public void Wal_files_survive_closing_every_connection()      // abrir, gravar, fechar tudo + SqliteConnection.ClearAllPools() → -wal e -shm existem
[Fact] public void Migrate_creates_the_schema_and_sets_user_version() // user_version = 1; tabelas session…aggregate_1m existem
[Fact] public void Migrate_is_idempotent()                            // rodar 2× → 1, sem erro
```

- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar.** `PersistentWal.Enable(SqliteConnection)` usa `connection.Handle!.DangerousGetHandle()`; resultado ≠ 0 lança `InvalidOperationException` com o código.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Commit** — `feat: add durable SQLite connection, persistent WAL and migrations`

---

### Task 4: Codec de quadros e `IMonitoringStore` em SQLite

**Files:**
- Create: `src/.../Persistence.Sqlite/Frames/FrameCodec.cs`
- Create: `src/.../Persistence.Sqlite/Monitoring/SqliteMonitoringStore.cs`, `Monitoring/MonitoringSql.cs` (SQL da classe, nada de SQL espalhado)
- Create: `src/.../Persistence.Sqlite/Frames/SqliteFrameReader.cs` (só o necessário para testes e para o Plano 03: `IReadOnlyList<Frame> ReadSegment(long segmentId)` e `SensorCatalog CatalogOf(long segmentId)`)
- Test: `tests/.../FrameCodecTests.cs`, `SqliteMonitoringStoreTests.cs`

**Interfaces (Produces):**
- `static class FrameCodec { byte[] Encode(IReadOnlyList<float> values); float[] Decode(ReadOnlySpan<byte> blob); }` — `float32` little-endian; `Decode` rejeita tamanho não múltiplo de 4.
- `sealed class SqliteMonitoringStore(SqliteDatabase db) : IMonitoringStore` — mantém **uma** conexão de escrita aberta (`IDisposable`); `StartSegment` faz upsert de cada sensor por `stable_key` e grava `segment_sensor`; `AppendFrame` grava o quadro e atualiza `segment.last_frame_ms`, na mesma transação.
- `sealed class SqliteFrameReader(SqliteDatabase db)` — usa `OpenReader()`.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Codec_round_trips_values_including_NaN()           // [1.5f, NaN, -3f] → idem (Review Focus 2)
[Fact] public void Codec_rejects_a_blob_whose_length_is_not_a_multiple_of_4()
[Fact] public void Store_writes_frames_that_read_back_in_order()
[Fact] public void Store_keeps_two_frames_with_the_same_timestamp()   // Review Focus 1
[Fact] public void Each_segment_decodes_with_its_own_catalog()        // seg A [/a,/b], seg B [/b,/c] → valores por chave corretos (Review Focus 3)
[Fact] public void Same_stable_key_maps_to_one_sensor_row_across_segments()
[Fact] public void EndSegment_and_EndSession_store_time_and_reason()
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: persist monitoring sessions, segments and frames`

---

### Task 5: Leitura somente leitura e retenção

**Files:**
- Create: `src/.../Persistence.Sqlite/Retention/SqliteRetentionStore.cs`
- Test: `tests/.../ReadOnlyAccessTests.cs`, `SqliteRetentionStoreTests.cs`

**Interfaces (Produces):** `sealed class SqliteRetentionStore(SqliteDatabase db) : IRetentionStore`.
- `AggregateMinutesBefore(cutoff)`: para cada minuto completo `< cutoff` ainda sem linha em `aggregate_1m`, calcula mín/méd/máx por sensor ignorando `NaN` (o cálculo é feito em C# sobre os quadros decodificados; minuto só com `NaN` não gera linha).
- `DeleteFramesBefore(cutoff)`: `DELETE FROM frame WHERE ts_ms < @cutoff` em lotes de 10 000.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Reader_opens_while_the_writer_is_closed_and_the_folder_is_not_writable()
// cria banco com dados, fecha tudo, nega WriteData/AppendData/CreateFiles na PASTA para o usuário atual
// (FileSystemAclExtensions), torna o -shm somente leitura → OpenReader() + SELECT COUNT(*) FROM frame devolve o total.
[Fact] public void Reader_sees_frames_committed_by_an_open_writer()
[Fact] public void Aggregation_computes_min_avg_max_per_sensor_and_minute_ignoring_NaN()
// minuto 12:00: /a = [1, 3, NaN] → min 1, avg 2, max 3
[Fact] public void Aggregation_skips_the_current_incomplete_minute()
[Fact] public void Aggregation_does_not_duplicate_minutes_already_aggregated()
[Fact] public void DeleteFramesBefore_removes_only_older_frames()
```

O teste de ACL restaura as permissões no `Dispose`.

- [ ] **Step 2–4:** falhar → implementar → passar. **Se o primeiro teste falhar por limitação do SQLite**, parar e registrar: o plano B do ADR 0004 (named pipe) vira decisão do dono.
- [ ] **Step 5: Commit** — `feat: add read-only access check and retention store`

---

### Task 6: Adapter LHM com perfis de placa e status do driver

**Files:**
- Create: `src/.../Sensors.Lhm/Profiles/BoardProfile.cs`, `BoardProfileCatalog.cs`, `Profiles/asus-tuf-gaming-a620m-plus-wifi.json` (embedded)
- Create: `src/.../Sensors.Lhm/LhmSensorSource.cs`, `SensorSelection.cs`, `LhmDriverProbe.cs`
- Create: `tests/PcMonitor.Infrastructure.Sensors.Lhm.Tests/` (novo projeto, com `AGENTS.md`/`CLAUDE.md`; NSubstitute)
- Test: `BoardProfileCatalogTests.cs`, `SensorSelectionTests.cs`

**Perfil `asus-tuf-gaming-a620m-plus-wifi.json`:**

```json
{
  "boardProduct": "TUF GAMING A620M-PLUS WIFI",
  "exclude": [
    "^/amdcpu/0/voltage/",
    "^/amdcpu/0/power/[1-9]",
    "^/amdcpu/0/load/([2-9]|1[0-9])$",
    "^/memory/dimm/\\d+/temperature/[1-5]$"
  ],
  "sensors": [
    { "key": "/amdcpu/0/temperature/2", "name": "CPU (Tctl/Tdie)", "role": "CpuTemperature" },
    { "key": "/amdcpu/0/power/0", "name": "CPU Package", "role": "CpuPackagePower" },
    { "key": "/amdcpu/0/load/0", "name": "CPU Total", "role": "CpuLoad" },
    { "key": "/lpc/nct6799d/0/voltage/0", "name": "Vcore", "role": "Vcore" },
    { "key": "/lpc/nct6799d/0/voltage/1", "name": "+5V", "role": "Rail5V", "multiplier": 5 },
    { "key": "/lpc/nct6799d/0/voltage/3", "name": "+3.3V", "role": "Rail3V3" },
    { "key": "/lpc/nct6799d/0/voltage/4", "name": "+12V", "role": "Rail12V", "multiplier": 12 }
  ]
}
```

**Interfaces (Produces):**
- `sealed record BoardProfile(string BoardProduct, IReadOnlyList<Regex> Exclude, IReadOnlyList<ProfileSensor> Sensors)`; `sealed record ProfileSensor(string Key, string Name, SensorRole Role, float Multiplier = 1)`.
- `static class BoardProfileCatalog { BoardProfile? FindFor(string? boardProduct); }` — compara sem diferenciar maiúsculas e ignorando espaços nas pontas.
- `static class SensorSelection { IReadOnlyList<SelectedSensor> Select(IEnumerable<ISensor> sensors, BoardProfile? profile, bool driverAvailable); }` — `sealed record SelectedSensor(ISensor Sensor, SensorDescriptor Descriptor, float Multiplier)`. Regras, em ordem: só `SensorType` ∈ {Temperature, Voltage, Power, Load, Fan}; sem driver, só `Load`; aplicar `exclude`; nome, papel e multiplicador do perfil quando a chave bate; ordem estável por `StableKey`.
- `sealed class LhmDriverProbe` — `bool IsAvailable` = `PawnIo.IsInstalled` (confirmar na execução se é estático; se não, instanciar como a LHM faz).
- `sealed class LhmSensorSource : ISensorSource, IDisposable` — `Open()` abre `Computer` (CPU, Motherboard, Memory), lê o produto da placa por `computer.SMBios.Board.ProductName`, escolhe o perfil, seleciona sensores e devolve status: sem driver → `DriverMissing`; driver presente sem nenhum hardware `SuperIO` → `BoardNotDetected`; senão `Ok`. `Read()` atualiza o hardware e devolve `valor × multiplicador` (ou `NaN`).

- [ ] **Step 1: Testes que falham** (com `ISensor` falsos via NSubstitute)

```csharp
[Fact] public void Profile_is_found_by_board_product_ignoring_case_and_padding()  // " tuf gaming a620m-plus wifi " → perfil
[Fact] public void Unknown_board_has_no_profile()
[Fact] public void Selection_keeps_only_temperature_voltage_power_load_and_fan()
[Fact] public void Selection_without_driver_keeps_only_load_sensors()
[Fact] public void Selection_applies_profile_names_roles_and_multipliers()       // voltage/4 → "+12V", Rail12V, 12
[Fact] public void Selection_drops_excluded_keys()                               // /amdcpu/0/voltage/0 excluída
[Fact] public void Every_embedded_profile_parses_and_has_unique_keys_and_roles()
```

- [ ] **Step 2–4:** falhar → implementar → passar. `LhmSensorSource` em si não tem teste unitário (é a borda com o hardware); ela é verificada na Task 9.
- [ ] **Step 5:** adicionar o novo projeto de testes ao `PcMonitor.slnx` (o `Test-AgentDocs.ps1` já exige o `AGENTS.md` dele).
- [ ] **Step 6: Commit** — `feat: add LibreHardwareMonitor sensor source with board profiles`

---

### Task 7: Host do coletor — amostragem, watchdog, eventos do Windows

**Files:**
- Create: `src/PcMonitor.Collector/Configuration/CollectorOptions.cs`
- Create: `src/PcMonitor.Collector/Sampling/SamplingWorker.cs` (BackgroundService), `Sampling/SamplerThread.cs`, `Sampling/StallWatchdog.cs`, `Sampling/IProcessTerminator.cs`
- Create: `src/PcMonitor.Collector/Hosting/PcMonitorServiceLifetime.cs`, `Hosting/ServiceSignals.cs`
- Create: `src/PcMonitor.Collector/Retention/RetentionWorker.cs`
- Modify: `src/PcMonitor.Collector/Program.cs`, `appsettings.json`
- Create: `tests/PcMonitor.Collector.Tests/` (novo, `AGENTS.md`/`CLAUDE.md`)
- Test: `StallWatchdogTests.cs`, `ServiceSignalsTests.cs`, `CollectorOptionsTests.cs`

**Interfaces (Produces):**
- `sealed class CollectorOptions` (`Collector`): `DatabasePath` (padrão `%ProgramData%\PcMonitor\pcmonitor.db`, expandido), `SampleInterval` = 1 s, `StallTimeout` = 10 s, `RawRetention` = 14 d, `RetentionInterval` = 1 h. Validação: intervalo entre 200 ms e 60 s; `StallTimeout` ≥ 5 × intervalo.
- `sealed class ServiceSignals` — fila thread-safe de `ServiceSignal` (`enum ServiceSignal { Shutdown, Suspend, Resume }`) e `bool TryDequeue(out ServiceSignal)`; `EndReason StopReason` (padrão `ServiceStopped`, vira `SystemShutdown` ao receber `Shutdown`).
- `sealed class PcMonitorServiceLifetime : WindowsServiceLifetime` — `CanShutdown = true`, `CanHandlePowerEvent = true`; `OnShutdown()` → `signals.Enqueue(Shutdown)` e depois `base.OnShutdown()`; `OnPowerEvent(Suspend)` → `Suspend`; `ResumeSuspend`/`ResumeAutomatic` → `Resume`. Registrada só quando `WindowsServiceHelpers.IsWindowsService()`.
- `sealed class StallWatchdog(TimeSpan timeout, TimeProvider time, IProcessTerminator terminator, ILogger logger)` — `void Beat()`; `void Check()` → se `agora − último beat > timeout`, loga `SamplerStalled` (EventId 1001) e chama `terminator.Terminate(reason)`. Produção: `Environment.FailFast`, para o SCM aplicar as ações de recuperação.
- `SamplerThread` — thread dedicada (`IsBackground = true`, nome `PcMonitor.Sampler`): `recorder.Start()`, depois laço a cada `SampleInterval` com tempo de referência (não acumula atraso): processa sinais (`Suspend` → `BreakSegment(Suspended)`; `Resume` → nada, o próximo `RecordOnce` abre segmento), `RecordOnce()`, `watchdog.Beat()`. No cancelamento: `recorder.Stop(signals.StopReason)`.
- `SamplingWorker` inicia a thread e roda `watchdog.Check()` a cada segundo **na thread do host**.
- `RetentionWorker` — `PeriodicTimer(RetentionInterval)` chama `ApplyRetention.Run()`, loga o resultado; exceção é logada e não derruba o host.
- Composição em `Program.cs`: `SqliteDatabase` → `SchemaMigrator.Migrate()` antes de `Run()`; registros de `ISensorSource`, `IMonitoringStore`, `IRetentionStore`, `MonitoringRecorder`, workers; logging no Event Log.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Watchdog_terminates_when_no_beat_within_the_timeout()   // FakeTimeProvider +11 s → Terminate chamado
[Fact] public void Watchdog_stays_quiet_while_beats_arrive()
[Fact] public void Stop_reason_is_SystemShutdown_after_a_shutdown_signal()
[Fact] public void Stop_reason_defaults_to_ServiceStopped()
[Fact] public void Options_reject_a_stall_timeout_shorter_than_five_intervals()
```

(Review Focus 4 e 5 já estão testados no `MonitoringRecorder`, Task 2.)

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Verificação local (console, terminal de administrador do usuário)** — `dotnet run --project src/PcMonitor.Collector` por 30 s com `Collector__DatabasePath` apontando para uma pasta temporária; Ctrl+C. Esperado: uma sessão `ServiceStopped`, ~30 quadros, catálogo com `+12V` ≈ 12,2 e `CPU (Tctl/Tdie)` entre 30 e 90. Claude confere com uma consulta SQL.
- [ ] **Step 6: Commit** — `feat: run sampling with watchdog and Windows service signals`

---

### Task 8: Instalação do serviço, docs e limpeza

**Files:**
- Create: `scripts/Install-Collector.ps1`, `scripts/Uninstall-Collector.ps1`
- Modify: `docs/architecture/overview.md`, `flows.md`, `data-model.md` (status ✅ no que ficou pronto; data-model mostra o schema real); `docs/adr/0011-segments-and-gaps.md` (segmentos, lacunas e watchdog via FailFast)
- Modify: `AGENTS.md` raiz (TFMs `net10.0`/`net10.0-windows`), AGENTS dos projetos tocados
- Modify: `tests/PcMonitor.Architecture.Tests/LayerDependencyTests.cs` (Domain: comparar com a lista de assemblies do framework compartilhado em vez de `StartsWith("System")` — minor adiado do Plano 01)

**`Install-Collector.ps1`** (exige administrador; aborta com mensagem clara se não for):
1. Parâmetro `-SourcePath` (padrão: `artifacts/collector`, gerado por `dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector`).
2. PawnIO: se `Get-Service PawnIO` falhar → `winget install --id namazso.PawnIO -e --accept-source-agreements` (ADR 0010); se o winget não existir, avisa e continua (o coletor registra `DriverMissing`).
3. Para o serviço se existir; copia para `%ProgramFiles%\PcMonitor\Collector`.
4. Cria `%ProgramData%\PcMonitor` com ACL: SYSTEM e Administradores controle total, herança desativada (a conta do painel entra no Plano 05).
5. `New-Service -Name PcMonitor.Collector -BinaryPathName … -StartupType Automatic -DisplayName "PC Monitor Collector"`; `sc.exe failure PcMonitor.Collector reset= 86400 actions= restart/5000/restart/5000/restart/30000`; `sc.exe failureflag PcMonitor.Collector 1`; inicia.
6. Imprime o status e o caminho do banco.

`Uninstall-Collector.ps1`: para e remove o serviço e os binários; **mantém** o banco, a não ser com `-RemoveData` (pede confirmação).

- [ ] **Step 1:** escrever os scripts e validar a sintaxe com `pwsh -NoProfile -Command "[System.Management.Automation.Language.Parser]::ParseFile(...)"` (sem erros).
- [ ] **Step 2:** docs, ADR 0011, AGENTS e a checagem do Domain. Provar que a nova checagem falha com uma referência temporária do Domain ao pacote `System.IO.Abstractions` (nome começa com `System.`, mas não é do framework) e uma classe que o use; depois reverter.
- [ ] **Step 3:** suíte completa + `Test-AgentDocs.ps1` + jscpd.
- [ ] **Step 4: Commit** — `feat: add collector install scripts and update docs`

---

### Task 9: Instalar e verificar nesta máquina

Não produz código; prova que o coletor funciona de verdade.

- [ ] **Step 1:** `dotnet publish src/PcMonitor.Collector -c Release -o artifacts/collector`.
- [ ] **Step 2:** o **usuário** roda `pwsh scripts/Install-Collector.ps1` num terminal de administrador.
- [ ] **Step 3: Verificar** (Claude, leitura): `Get-Service PcMonitor.Collector` = Running; após 2 min, a sessão aberta tem `source_status = Ok`, ~120 quadros e o catálogo tem os 7 papéis do perfil; o tamanho de `pcmonitor.db` + `-wal` é coerente com ~20 MB/dia; o Event Log tem a fonte `PcMonitor.Collector` sem erros.
- [ ] **Step 4: Reiniciar o serviço** (`Restart-Service`, pelo usuário) → a sessão anterior termina com `ServiceStopped` e uma nova começa.
- [ ] **Step 5:** registrar o resultado no fim deste plano, numa seção `## Resultado da instalação`, e commitar.

## Critério de pronto

- Suíte .NET, frontend, contrato, agent docs e jscpd verdes.
- Coletor instalado como serviço nesta máquina, gravando 1 quadro/s com os papéis do perfil, e sobrevivendo a reinício.
- Toda decisão nova registrada (ADR 0011 e rulings do ledger).
