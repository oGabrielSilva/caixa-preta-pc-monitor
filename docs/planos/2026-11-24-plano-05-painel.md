# Plano 05 — Painel

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** o painel aprovado nos mockups, ligado aos dados reais: **Ao vivo** (valores atuais, faixas seguras, últimos 10 min, atualização a cada segundo), **Incidentes** (resumo, quedas por dia, lista e o detalhe de cada queda com diagnóstico, linha do tempo e telemetria) e **Histórico** (sensor × período). Tudo instalado como o segundo serviço Windows, sem privilégio, lendo o banco em somente leitura.

**Architecture:** a Application ganha **portas de leitura** (`ILiveReader`, `IIncidentQuery`, `IHistoryReader`, `IStorageStatus`) e a lógica pura de leitura (estado do "ao vivo", acompanhamento de quadros novos, baldes do histórico). O SQLite implementa as portas só com `OpenReader()`. O Dashboard compõe: endpoints REST + um fluxo **SSE** para o ao vivo, DTOs em C# (o TypeScript é gerado) e a SPA. O frontend segue o `AGENTS.md` dele: páginas só compõem, componentes por área, composables são o único acesso à API, lógica de apresentação em `utils/` puros e testados. Os códigos do diagnóstico chegam como **códigos + parâmetros** e são traduzidos no frontend (pt-BR/en).

**Tech Stack:** .NET 10 · ASP.NET Core minimal API (`TypedResults.ServerSentEvents`) · Microsoft.Data.Sqlite + Dapper · Nuxt 4.6 SPA · Nuxt UI 4.13 (`UChart` sobre `@tanstack/charts` 1.5.0) · openapi-typescript/openapi-fetch · xUnit v3, Shouldly, FakeTimeProvider · Vitest.

**Spec:** [estudo](../estudos/2026-11-24-estudo-arquitetura.md) §3, §7 (Permissões), §8 · mockups aprovados pelo dono em 24/11/2026 (Ao vivo, Incidentes, detalhe da queda, Histórico) · [diagnosis.md](../architecture/diagnosis.md) (catálogo de códigos) · ADRs [0003](../adr/0003-collector-dashboard-split-least-privilege.md), [0004](../adr/0004-sqlite-wal-full-sync-frame-rows.md), [0007](../adr/0007-nuxt-spa-static-with-nuxt-ui.md), [0008](../adr/0008-openapi-generated-typescript-contract.md), [0012](../adr/0012-explainable-versioned-diagnosis.md).

## Fatos verificados (24/11/2026)

- O leitor abre o banco com a pasta inteira **sem escrita** quando `-wal`/`-shm` existem (WAL persistente; `ReadOnlyAccessTests`, Plano 02). Falta confirmar com o coletor **gravando** ao mesmo tempo, sob a conta do painel (Task 13).
- `aggregate_1m` é preenchido pela retenção **uma vez por hora**: a última hora pode não ter agregados.
- Os dados de eventos (`incident_event.data_json`, `incident.data_json`) contêm nome do computador e do usuário (evento 1074).
- O `UChart` usa escala de pontos no eixo X: rótulos repetidos se fundem (visto nos mockups). Os pontos precisam de um índice único.
- O `System.Text.Json` lança exceção ao serializar `NaN`; os quadros usam `NaN` para sensor sem leitura.
- Mockups: `app/mock/`, `app/components/mockup/`, as páginas e `history.vue` estão alterados **sem commit**; `@tanstack/charts` 1.5.0 já está no `package.json`.

## Global Constraints

- Tudo dos planos anteriores continua valendo: camadas (hosts → infra → application → domain), CPM, warnings como erro, testes antes, inglês no código, português nos docs, jscpd com threshold 0, `AGENTS.md` + `CLAUDE.md` (`@AGENTS.md`) em toda pasta de projeto nova.
- **O painel nunca escreve no banco** e nunca migra o schema: só `SqliteDatabase.OpenReader()`.
- **Privacidade:** a API não devolve nenhum `data_json` nem campo derivado de texto livre de evento. Eventos saem só com `provider`, `eventId`, `loggedAt`, `level`. Nada sai da máquina: sem CDN, sem telemetria, Kestrel só em `127.0.0.1`.
- Serviço `PcMonitor.Dashboard`, conta virtual **`NT SERVICE\PcMonitor.Dashboard`** (o nome da conta virtual é o nome do serviço), início automático, porta 5280.
- Estado do armazenamento: arquivo ausente → `Missing`; `user_version` menor que o do binário → `SchemaTooOld`; maior → `SchemaTooNew`; igual → `Ready`. Os endpoints de dados respondem **503** (ProblemDetails, `type` = `storage.missing` / `storage.schema_too_old` / `storage.schema_too_new`) quando não está `Ready`.
- Ao vivo: `Recording` = sessão aberta e último quadro há ≤ **5 s**; `Stale` = sessão aberta e quadro mais antigo; `Stopped` = sessão encerrada; `NoData` = nenhuma sessão. Janela: **10 min**. O SSE consulta o banco a cada **1 s** e envia o estado a cada **5 s**.
- Histórico: períodos `Hour` (balde 1 min), `Day` (15 min), `Week` (1 h), `TwoWeeks` (2 h); baldes alinhados em múltiplos UTC do tamanho; mínimo = menor mínimo, máximo = maior máximo, média = média das médias por minuto. Minutos sem agregado são calculados dos quadros.
- Faixas e limites mostrados vêm do C#: rails de `RailTolerance.Atx`, CPU de `DiagnosisThresholds` (`CpuAtLimitCelsius` = 95 → atenção, `CpuCriticalCelsius` = 100 → fora da faixa). **Nenhum limiar é repetido no TypeScript.**
- Formato de tempo na API: datas de entidades como `DateTimeOffset` (ISO 8601); pontos de série como `t` = Unix ms (número). Valores de série `float?` (`NaN` → `null`). Enums como texto.
- Todo texto visível vem de `i18n/locales/{pt-BR,en}.json`, com as mesmas chaves; todo `FindingCode` tem texto (garantido em tempo de compilação, Task 8).
- Visual: o dos mockups aprovados (tema escuro padrão, cards, `UChart`, cores semânticas `success`/`warning`/`error` só pelo significado).

## Review Focus

1. **Banco ausente ou de outra versão** (painel instalado antes do coletor, ou um atualizado sem o outro): API responde 503 com o código certo, `/api/health` continua 200 e mostra o estado, e a SPA mostra um aviso em vez de quebrar. Testes nas Tasks 4 e 8.
2. **Coletor parado, PC voltando da suspensão, troca de segmento no meio do SSE:** o estado muda para `Stale`/`Stopped` sem recarregar a página; um segmento novo faz o cliente recarregar o retrato; o fim da conexão encerra o laço no servidor. Testes nas Tasks 2 e 6.
3. **Incidente sem diagnóstico, sem telemetria ou com telemetria que não é do momento da queda** (os 16 reais): o detalhe abre, sem gráfico ou com o aviso, e com as limitações. Testes nas Tasks 3 e 5.
4. **Sensor ausente ou sem leitura** (`NaN`, placa sem perfil, `DriverMissing`): JSON com `null`, card com "—", linha do gráfico interrompida, nada de exceção. Testes nas Tasks 3, 6 e 8.
5. **Histórico com buracos e com a última hora sem agregado:** baldes certos (incluindo minutos vindos dos quadros), linhas interrompidas onde o PC estava desligado, horários exibidos no fuso local. Testes nas Tasks 3, 7 e 8.

---

### Task 1: Catálogo de códigos do diagnóstico

**Files:**
- Create: `src/PcMonitor.Domain/Diagnosis/FindingCodes.cs`
- Modify: `src/PcMonitor.Domain/Diagnosis/DiagnosisEngine.cs` e `Rules/*.cs` (literais → constantes)
- Test: `tests/PcMonitor.Domain.Tests/Diagnosis/FindingCodesTests.cs`

**Interfaces (Produces):** `public static class FindingCodes` com uma `public const string` por código de [diagnosis.md](../architecture/diagnosis.md) (ex.: `RailOutOfTolerance = "rail.out_of_tolerance"`, `BugcheckNone = "bugcheck.none"`) e `public static IReadOnlyList<string> All` (ordem de declaração).

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void All_lists_every_constant_once()  // reflection sobre os const string públicos == All, sem repetição
[Fact] public void Codes_match_the_documented_catalogue() // All.ShouldBe([...os 26 códigos de diagnosis.md...], ignoreOrder: true)
[Fact] public void Every_code_a_rule_emits_is_in_the_catalogue() // roda DiagnosticRules.All + DiagnosisEngine sobre os builders de cenário de EvidenceBuilder (quente, rail fora, idle, sleep, bugcheck, whea, sem telemetria) e verifica que todo Finding.Code ∈ All
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Domain.Tests` → FAIL (`FindingCodes` não existe).
- [ ] **Step 3:** criar `FindingCodes` e trocar os literais nas regras e no motor.
- [ ] **Step 4:** mesmo comando → PASS, e os testes de regras existentes continuam verdes sem mudança.
- [ ] **Step 5:** commit `refactor: name the diagnosis finding codes in one catalogue`.

---

### Task 2: Portas de leitura e lógica pura (Application)

**Files:**
- Create: `src/PcMonitor.Application/Storage/{IStorageStatus.cs, StorageState.cs}`
- Create: `src/PcMonitor.Application/Live/{ILiveReader.cs, LiveSnapshot.cs, SessionView.cs, LiveState.cs, LiveStateRule.cs, LiveFeed.cs, LiveUpdate.cs}`
- Create: `src/PcMonitor.Application/Incidents/{IIncidentQuery.cs, IncidentSummary.cs, IncidentDetail.cs, RelatedEventView.cs, StoredDiagnosis.cs}`
- Create: `src/PcMonitor.Application/History/{IHistoryReader.cs, HistorySensor.cs, MinuteAggregate.cs, HistoryRange.cs, HistoryBucket.cs, HistoryResult.cs, QueryHistory.cs}`
- Test: `tests/PcMonitor.Application.Tests/Live/{LiveStateRuleTests.cs, LiveFeedTests.cs}`, `tests/PcMonitor.Application.Tests/History/QueryHistoryTests.cs`

**Interfaces (Produces):**
- `enum StorageState { Ready, Missing, SchemaTooOld, SchemaTooNew }`; `interface IStorageStatus { StorageState Check(); }`
- `sealed record SessionView(long Id, DateTimeOffset StartedAt, SensorSourceStatus SourceStatus, string? BoardProduct, DateTimeOffset? EndedAt, EndReason? EndReason)`
- `sealed record LiveSnapshot(SessionView Session, long SegmentId, SensorCatalog Catalog, IReadOnlyList<Frame> Frames)`
- `interface ILiveReader { LiveSnapshot? Latest(TimeSpan window); IReadOnlyList<Frame> FramesAfter(long segmentId, DateTimeOffset after); (long? SegmentId, SessionView? Session) Head(); }`: `Latest` = último segmento da última sessão, quadros em `[último − window, último]`; `Head` = segmento e sessão mais recentes, sem quadros.
- `enum LiveState { NoData, Recording, Stale, Stopped }`; `static class LiveStateRule { static readonly TimeSpan StaleAfter = 5 s; static LiveState Of(SessionView? session, DateTimeOffset? lastFrameAt, DateTimeOffset now); }`
- `abstract record LiveUpdate` com `FramesAppended(long SegmentId, IReadOnlyList<Frame> Frames)`, `SegmentChanged(long SegmentId)`, `StateChanged(LiveState State)`.
- `sealed class LiveFeed(ILiveReader reader, TimeProvider time, ILogger<LiveFeed> logger) { static readonly TimeSpan PollInterval = 1 s, StateInterval = 5 s; IAsyncEnumerable<LiveUpdate> Follow(long? segmentId, DateTimeOffset? after, CancellationToken ct); }`: a cada `PollInterval` (via `Task.Delay(…, time, ct)`) lê `Head()`; segmento diferente → `SegmentChanged` e passa a seguir o novo; senão `FramesAfter` → `FramesAppended` se houver quadros; a cada `StateInterval`, `StateChanged` com `LiveStateRule.Of`. Exceção de leitura (banco ocupado) é registrada com `[LoggerMessage]` e a leitura é tentada de novo no próximo ciclo.
- `IncidentSummary(long Id, DateTimeOffset LoggedAt, DateTimeOffset? EstimatedFailureAt, FailureTimeSource Source, int SleepState, long BugcheckCode, bool HasTelemetry, Hypothesis? TopHypothesis, Confidence? Confidence, IReadOnlyList<string> ObservationCodes)`; `Confidence == null` = ainda sem diagnóstico; `HasTelemetry` = `telemetry_end_ms` não nulo.
- `RelatedEventView(string Provider, int EventId, DateTimeOffset LoggedAt, int Level)` (sem dados do evento, por construção).
- `StoredDiagnosis(int EngineVersion, DateTimeOffset DiagnosedAt, IReadOnlyList<HypothesisScore> Ranking, IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Limitations)`
- `IncidentDetail(IncidentSummary Summary, DateTimeOffset? LastAliveAt, DateTimeOffset? TelemetryEndAt, IReadOnlyList<RelatedEventView> Events, StoredDiagnosis? Diagnosis, TelemetryWindow? Telemetry)` (`TelemetryWindow` do Domain, a mesma janela de 120 s que o diagnóstico usa).
- `interface IIncidentQuery { IReadOnlyList<IncidentSummary> List(); IncidentDetail? Find(long id); }` (`List` em ordem decrescente de `LoggedAt`).
- `HistorySensor(string StableKey, string Name, SensorKind Kind, string Unit, SensorRole Role)`; `MinuteAggregate(DateTimeOffset Minute, double Min, double Avg, double Max)`; `interface IHistoryReader { IReadOnlyList<HistorySensor> Sensors(); IReadOnlyList<MinuteAggregate> Minutes(string stableKey, DateTimeOffset from, DateTimeOffset to); }`
- `enum HistoryRange { Hour, Day, Week, TwoWeeks }`; `HistoryBucket(DateTimeOffset Start, double Min, double Avg, double Max)`; `HistoryResult(HistorySensor Sensor, DateTimeOffset From, DateTimeOffset To, TimeSpan Bucket, IReadOnlyList<HistoryBucket> Buckets)`
- `sealed class QueryHistory(IHistoryReader reader, TimeProvider time) { HistoryResult? Run(string stableKey, HistoryRange range); }` (`null` = sensor desconhecido); `static (TimeSpan Span, TimeSpan Bucket) Dimensions(HistoryRange range)`.

- [ ] **Step 1: Testes que falham**

```csharp
// LiveStateRuleTests
[Fact] public void No_session_is_no_data()                         // Of(null, null, now) == NoData
[Fact] public void Open_session_with_a_frame_5s_old_is_recording() // == Recording
[Fact] public void Open_session_with_a_frame_6s_old_is_stale()     // == Stale (coletor travado ou PC voltando)
[Fact] public void Ended_session_is_stopped()                      // EndedAt != null → Stopped, mesmo com quadro recente

// LiveFeedTests (FakeTimeProvider + fake ILiveReader escrito à mão)
[Fact] public async Task New_frames_are_appended_once()             // dois ciclos, um quadro novo no segundo → um FramesAppended com 1 quadro
[Fact] public async Task A_new_segment_is_announced_and_followed()  // Head muda 7 → 8 → SegmentChanged(8); depois lê FramesAfter(8, …)
[Fact] public async Task State_is_sent_every_five_seconds()         // 5 avanços de 1 s → um StateChanged
[Fact] public async Task A_failed_read_does_not_end_the_feed()      // FramesAfter lança uma vez → o próximo ciclo entrega os quadros
[Fact] public async Task Cancellation_ends_the_feed()               // cancelar → o enumerador termina sem exceção

// QueryHistoryTests
[Theory] [InlineData(Hour, 60, 1)] [InlineData(Day, 1440, 15)] [InlineData(Week, 10080, 60)] [InlineData(TwoWeeks, 20160, 120)]
public void Range_sets_span_and_bucket(HistoryRange range, int spanMinutes, int bucketMinutes)
[Fact] public void Buckets_take_min_of_min_max_of_max_and_mean_of_averages() // 15 minutos (min 11,9..12,0; avg 12,1/12,3; max …) → 1 balde Day com os três valores esperados
[Fact] public void Buckets_align_to_utc_multiples()               // minuto 10:07 com balde de 15 min → Start 10:00Z
[Fact] public void Minutes_without_data_produce_no_bucket()        // buraco de 2 h → nenhum balde nesse trecho
[Fact] public void Unknown_sensor_returns_null()
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Application.Tests` → FAIL (tipos não existem).
- [ ] **Step 3:** implementar os tipos acima. `QueryHistory.Run`: `To` = agora, `From` = `To − Span`, `Minutes(...)`, agrupar por `Start = floor(minuto / Bucket)`.
- [ ] **Step 4:** mesmo comando → PASS.
- [ ] **Step 5:** commit `feat: add the read ports and live/history logic for the dashboard`.

---

### Task 3: Leitores SQLite e suporte de teste compartilhado

**Files:**
- Create: `tests/PcMonitor.Tests.Support/` (biblioteca de classes, não é projeto de teste; `AGENTS.md`, `CLAUDE.md`), movendo para ela `TempDatabase` e `WriteDenied` de `tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests/Support/` e acrescentando `DatabaseSeed` (grava sessão, segmento, quadros, incidente com eventos e diagnóstico pelos stores reais). Os dois projetos de teste passam a referenciá-la.
- Modify: `src/PcMonitor.Infrastructure.Persistence.Sqlite/SqliteDatabase.cs` (`public static string DefaultPath` = `%ProgramData%\PcMonitor\pcmonitor.db`; `int? SchemaVersion()` pelo leitor), `Migrations/SchemaMigrator.cs` (`public static int LatestVersion`), `src/PcMonitor.Collector/Configuration/CollectorOptions.cs` (usa `SqliteDatabase.DefaultPath`)
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/Storage/SqliteStorageStatus.cs`, `Live/{SqliteLiveReader.cs, LiveSql.cs}`, `Incidents/{SqliteIncidentQuery.cs, IncidentQuerySql.cs}`, `History/{SqliteHistoryReader.cs, HistorySql.cs}`
- Create: `src/PcMonitor.Infrastructure.Persistence.Sqlite/Diagnosis/TelemetryWindowLoader.cs`: extrai de `SqliteDiagnosisStore` a montagem da janela de 120 s, para o diagnóstico e o painel usarem o mesmo código.
- Test: `tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests/{SqliteStorageStatusTests.cs, SqliteLiveReaderTests.cs, SqliteIncidentQueryTests.cs, SqliteHistoryReaderTests.cs}`

**Interfaces:**
- Consumes: as portas da Task 2; `FrameCodec`, `DiagnosisJson` (ganha a desserialização do formato fixado por `Stored_json_has_the_shape_the_dashboard_reads`).
- Produces: `SqliteStorageStatus(SqliteDatabase) : IStorageStatus`, `SqliteLiveReader(SqliteDatabase) : ILiveReader`, `SqliteIncidentQuery(SqliteDatabase) : IIncidentQuery`, `SqliteHistoryReader(SqliteDatabase) : IHistoryReader`. Todos usam só `OpenReader()`.

- [ ] **Step 1: Testes que falham**

```csharp
// SqliteStorageStatusTests
[Fact] public void Missing_file_is_missing()                 // caminho inexistente → Missing, e o arquivo continua sem existir
[Fact] public void Current_schema_is_ready()                 // TempDatabase.Migrated() → Ready
[Fact] public void Older_schema_is_too_old()                  // Migrate(upTo: LatestVersion - 1) → SchemaTooOld
[Fact] public void Newer_schema_is_too_new()                  // PRAGMA user_version = LatestVersion + 1 → SchemaTooNew

// SqliteLiveReaderTests
[Fact] public void Latest_returns_the_last_segment_within_the_window()   // 900 quadros de 1 s, window 10 min → 601 quadros, catálogo do segmento
[Fact] public void Latest_is_null_without_sessions()
[Fact] public void Frames_after_returns_only_newer_frames_of_that_segment()
[Fact] public void Head_follows_a_new_segment()               // abrir segmento 2 → Head().SegmentId == 2
[Fact] public void Reads_work_with_the_folder_write_denied()  // WriteDenied(temp.Folder) depois de fechar o writer → Latest não lança

// SqliteIncidentQueryTests
[Fact] public void List_is_newest_first_with_diagnosis_fields() // 2 incidentes, 1 diagnosticado → TopHypothesis/Confidence só nele; ObservationCodes = códigos das observações
[Fact] public void Undiagnosed_incident_has_null_confidence()
[Fact] public void Find_returns_events_without_any_event_data() // seed com 1074 contendo "DESKTOP-TEST"/"tester" → RelatedEventView só tem Provider/EventId/LoggedAt/Level; nenhum campo de IncidentDetail contém "DESKTOP-TEST" (serializar com System.Text.Json e procurar)
[Fact] public void Find_includes_the_same_telemetry_window_the_diagnosis_used() // == TelemetryWindowLoader direto
[Fact] public void Find_without_telemetry_has_null_window()
[Fact] public void Find_unknown_id_returns_null()

// SqliteHistoryReaderTests
[Fact] public void Sensors_are_distinct_by_stable_key_with_the_latest_name() // mesmo key em 2 segmentos com nomes diferentes → 1 item, nome do segmento mais novo
[Fact] public void Minutes_read_aggregates()                   // linhas em aggregate_1m → devolvidas como estão
[Fact] public void Minutes_without_aggregate_are_computed_from_frames() // 3 minutos agregados + 2 só com quadros → 5 minutos, os 2 últimos com min/avg/max dos quadros
[Fact] public void NaN_values_are_ignored_in_computed_minutes()
[Fact] public void Minutes_follow_the_sensor_ordinal_of_each_segment() // ordinal muda entre segmentos → valores certos
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests` → FAIL.
- [ ] **Step 3:** criar o projeto de suporte (sem `IsTestProject`; referências de pacote via CPM) e mover os helpers; criar os leitores. Histórico: agregados por `JOIN segment_sensor … JOIN sensor ON stable_key`; minutos com quadros e sem linha em `aggregate_1m` são decodificados no ordinal daquele segmento.
- [ ] **Step 4:** `dotnet test --solution PcMonitor.slnx` → PASS (os testes movidos continuam verdes).
- [ ] **Step 5:** commit `feat: read live frames, incidents and history from SQLite for the dashboard`.

---

### Task 4: Host do Dashboard como serviço

**Files:**
- Modify: `src/PcMonitor.Dashboard/Program.cs`, `PcMonitor.Dashboard.csproj` (+ `Microsoft.Extensions.Hosting.WindowsServices`, `Microsoft.Extensions.Logging.EventLog`)
- Modify: `src/PcMonitor.Dashboard/Configuration/DashboardOptions.cs` (`[Required] string DatabasePath` = `SqliteDatabase.DefaultPath`)
- Create: `src/PcMonitor.Dashboard/Configuration/DashboardServiceCollectionExtensions.cs` (`AddDashboard`: `AddWindowsService(ServiceName = "PcMonitor.Dashboard")`, `SqliteDatabase` singleton pelo `DatabasePath`, os quatro leitores, `LiveFeed`, `QueryHistory`, `TimeProvider.System`, `ConfigureHttpJsonOptions` com `JsonStringEnumConverter`, `AddProblemDetails`)
- Create: `src/PcMonitor.Dashboard/Api/StorageGate.cs` (filtro de endpoint: `IStorageStatus.Check() != Ready` → 503 ProblemDetails com o `type` do Global Constraints)
- Modify: `src/PcMonitor.Dashboard/Api/Health/{HealthEndpoints.cs, HealthResponse.cs}` (+ `StorageState Storage`)
- Test: `tests/PcMonitor.Dashboard.Tests/Hosting/DashboardHostTests.cs`, `Api/HealthEndpointsTests.cs`, `Api/StorageGateTests.cs`, `Support/DashboardFactory.cs` (`WebApplicationFactory<Program>` com `Dashboard:DatabasePath` apontando para um `TempDatabase` e web root temporário; substitui o código repetido de `SpaHostingTests`)

**Interfaces (Produces):** `RouteGroupBuilder.RequireStorage()` (aplica o `StorageGate`); `DashboardFactory.WithDatabase(TempDatabase)` / `.WithoutDatabase()` para as Tasks 5–7.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public async Task Health_reports_storage_state()          // sem banco → 200 { status: "ok", storage: "Missing" }; com banco → "Ready"
[Fact] public async Task Data_endpoints_answer_503_when_storage_is_missing() // GET num endpoint de teste com RequireStorage → 503, type "storage.missing"
[Fact] public async Task Schema_too_old_answers_503_with_its_type()
[Fact] public void Event_log_is_not_used_outside_a_windows_service() // providers de log do host de teste não incluem EventLogLoggerProvider
[Fact] public void Database_path_defaults_to_the_collector_path() // new DashboardOptions().DatabasePath == SqliteDatabase.DefaultPath
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Dashboard.Tests` → FAIL.
- [ ] **Step 3:** implementar. EventLog só quando `WindowsServiceHelpers.IsWindowsService()` (mesma regra do coletor; corrige os avisos de `WebRootPath` que os testes escreviam no Log de Eventos).
- [ ] **Step 4:** mesmo comando → PASS; abrir o Visualizador de Eventos não é necessário: o teste de providers cobre.
- [ ] **Step 5:** commit `feat: host the dashboard as a windows service with a storage gate`.

---

### Task 5: API de incidentes

**Files:**
- Create: `src/PcMonitor.Dashboard/Api/Incidents/{IncidentEndpoints.cs, IncidentSummaryResponse.cs, IncidentDetailResponse.cs, FindingResponse.cs, HypothesisScoreResponse.cs, EventResponse.cs, TelemetryResponse.cs}`
- Create: `src/PcMonitor.Dashboard/Api/Sensors/{SensorResponse.cs, RangeResponse.cs, SensorPresentation.cs, PointResponse.cs}` (compartilhados pelas Tasks 5–7)
- Create: `src/PcMonitor.Dashboard/Api/OpenApi/FindingCodeSchemaTransformer.cs`
- Modify: `src/PcMonitor.Dashboard/Api/ApiEndpoints.cs`, `openapi/PcMonitor.Dashboard.json` (gerado)
- Test: `tests/PcMonitor.Dashboard.Tests/Api/IncidentEndpointsTests.cs`, `Api/OpenApiContractTests.cs`

**Interfaces (Produces):**
- `GET /api/incidents` → `IncidentSummaryResponse[]` (`id, loggedAt, estimatedFailureAt?, timeSource, sleepState, bugcheckCode, hasTelemetry, topHypothesis?, confidence?, observations: FindingCode[]`).
- `GET /api/incidents/{id:long}` → `IncidentDetailResponse` (`summary, lastAliveAt?, telemetryEndAt?, events: EventResponse[], diagnosis?: { engineVersion, diagnosedAt, ranking: HypothesisScoreResponse[], findings: FindingResponse[], limitations: FindingResponse[] }, telemetry?: TelemetryResponse`) ou 404.
- `FindingResponse(string Code, FindingKind Kind, Hypothesis? Hypothesis, bool Supports, EvidenceStrength Strength, IReadOnlyDictionary<string, string> Parameters)`; o transformer publica `code` como **enum** com `FindingCodes.All`, então o TypeScript gerado é uma união de literais (`FindingCode`).
- `SensorResponse(string Key, string Name, SensorKind Kind, string Unit, SensorRole Role, RangeResponse? Range, float? Limit, float? Critical)`; `SensorPresentation.Describe(SensorDescriptor)` preenche `Range` pelos rails de `RailTolerance.Atx` e `Limit`/`Critical` pela CPU (`DiagnosisThresholds`).
- `TelemetryResponse(DateTimeOffset EndAt, bool AtFailure, IReadOnlyList<RoleSeriesResponse> Series)`; `RoleSeriesResponse(SensorRole Role, string Unit, RangeResponse? Range, float? Limit, float? Critical, IReadOnlyList<PointResponse> Points)`; `PointResponse(long T, float? V)`.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public async Task List_returns_incidents_newest_first()
[Fact] public async Task Detail_returns_diagnosis_events_and_telemetry()        // seed completo → ranking, findings com parameters, 3 eventos, série do Rail12V com range 11.4–12.6
[Fact] public async Task Detail_never_contains_event_data()                    // corpo JSON não contém "DESKTOP-TEST" nem "tester"
[Fact] public async Task Detail_of_an_undiagnosed_incident_without_telemetry_has_nulls() // diagnosis e telemetry null, 200
[Fact] public async Task Telemetry_sent_before_the_failure_is_flagged()        // Source != LastFrame → atFailure false
[Fact] public async Task Missing_values_are_null_not_NaN()                      // quadro com NaN → "v": null
[Fact] public async Task Unknown_incident_is_404()
[Fact] public void Finding_code_is_an_enum_of_the_catalogue_in_the_openapi_document() // lê openapi/PcMonitor.Dashboard.json: components.schemas.FindingResponse.properties.code.enum == FindingCodes.All
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Dashboard.Tests` → FAIL.
- [ ] **Step 3:** implementar; grupo `/api/incidents` com `RequireStorage()`; `dotnet build` regenera o OpenAPI.
- [ ] **Step 4:** mesmo comando → PASS.
- [ ] **Step 5:** commit `feat: expose incidents and their diagnosis through the dashboard api`.

---

### Task 6: API ao vivo e SSE

**Files:**
- Create: `src/PcMonitor.Dashboard/Api/Live/{LiveEndpoints.cs, LiveResponse.cs, SessionResponse.cs, FramePoint.cs, LiveFramesEvent.cs, LiveSegmentEvent.cs, LiveStateEvent.cs}`
- Modify: `src/PcMonitor.Dashboard/Api/ApiEndpoints.cs`, `openapi/PcMonitor.Dashboard.json`
- Test: `tests/PcMonitor.Dashboard.Tests/Api/LiveEndpointsTests.cs`

**Interfaces (Produces):**
- `GET /api/live` → `LiveResponse(LiveState State, SessionResponse? Session, long? SegmentId, IReadOnlyList<SensorResponse> Sensors, IReadOnlyList<FramePoint> Frames)`; `FramePoint(long T, IReadOnlyList<float?> Values)` alinhado com `Sensors`; `SessionResponse(DateTimeOffset StartedAt, SensorSourceStatus SourceStatus, string? BoardProduct)`.
- `GET /api/live/stream` → `text/event-stream` via `TypedResults.ServerSentEvents`, eventos `frames` (`LiveFramesEvent(long SegmentId, IReadOnlyList<FramePoint> Frames)`), `segment` (`LiveSegmentEvent(long SegmentId)`, o cliente recarrega `/api/live`) e `state` (`LiveStateEvent(LiveState State)`). Parâmetros `segmentId`, `after` (Unix ms) para continuar de onde o retrato parou. Os três tipos aparecem no OpenAPI (`Produces<…>(contentType: "text/event-stream")`) para o TypeScript gerado.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public async Task Live_returns_the_last_ten_minutes_with_sensor_ranges() // 900 quadros → 601 frames; sensor Rail12V com range; CpuTemperature com limit 95 e critical 100
[Fact] public async Task Live_without_sessions_is_no_data()                     // 200, state NoData, frames []
[Fact] public async Task Stream_sends_frames_written_after_the_snapshot()       // abre o stream com segmentId/after, grava 1 quadro pelo store, lê um evento "frames" com ele (timeout de 5 s)
[Fact] public async Task Stream_announces_a_new_segment()
[Fact] public async Task Closing_the_stream_stops_polling()                     // cancelar a requisição → LiveFeed termina (contador do reader fake para de subir)
[Fact] public async Task Stream_is_503_when_storage_is_missing()
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Dashboard.Tests` → FAIL.
- [ ] **Step 3:** implementar; o endpoint de stream mapeia `LiveFeed.FollowAsync(…, httpContext.RequestAborted)` para `SseItem<T>` com o tipo de evento.
- [ ] **Step 4:** mesmo comando → PASS.
- [ ] **Step 5:** commit `feat: stream live frames to the dashboard over server-sent events`.

---

### Task 7: API do histórico

**Files:**
- Create: `src/PcMonitor.Dashboard/Api/History/{HistoryEndpoints.cs, HistoryResponse.cs, BucketResponse.cs}`
- Modify: `src/PcMonitor.Dashboard/Api/ApiEndpoints.cs`, `openapi/PcMonitor.Dashboard.json`
- Test: `tests/PcMonitor.Dashboard.Tests/Api/HistoryEndpointsTests.cs`

**Interfaces (Produces):** `GET /api/history/sensors` → `SensorResponse[]` (via `SensorPresentation`); `GET /api/history?sensor={key}&range={HistoryRange}` → `HistoryResponse(SensorResponse Sensor, DateTimeOffset From, DateTimeOffset To, int BucketSeconds, IReadOnlyList<BucketResponse> Buckets)`, `BucketResponse(long T, double Min, double Avg, double Max)`; sensor desconhecido → 404; `range` inválido → 400.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public async Task Sensors_lists_recorded_sensors_with_ranges()
[Fact] public async Task History_returns_day_buckets_of_fifteen_minutes()     // BucketSeconds 900, baldes dos agregados + minutos dos quadros
[Fact] public async Task Unknown_sensor_is_404()
[Fact] public async Task Invalid_range_is_400()
```

- [ ] **Step 2:** `dotnet test --project tests/PcMonitor.Dashboard.Tests` → FAIL.
- [ ] **Step 3:** implementar (`QueryHistory` com `TimeProvider`; nos testes, `FakeTimeProvider` registrado na factory).
- [ ] **Step 4:** mesmo comando → PASS; `dotnet test --solution PcMonitor.slnx` → PASS.
- [ ] **Step 5:** commit `feat: serve sensor history buckets through the dashboard api`.

---

### Task 8: Base do frontend — contrato, composables e utilitários

**Files:**
- Delete: `app/mock/`, `app/components/mockup/`, `app/pages/incidents/[id].vue` do mockup (restaurar páginas antes de reescrever)
- Modify: `app/api/schema.d.ts` (`pnpm gen:api`), `package.json`/`pnpm-lock.yaml` (`@tanstack/charts` 1.5.0, já instalado), `pnpm-workspace.yaml` só se `pnpm install` acusar build bloqueado de pacote necessário
- Create: `app/composables/{useLive.ts, useIncidents.ts, useIncident.ts, useHistory.ts, useHistorySensors.ts, useStorageState.ts}`
- Create: `app/utils/{live-buffer.ts, incident-stats.ts, findings.ts, finding-keys.ts, sensor-status.ts, chart-rows.ts, event-keys.ts, format.ts}`
- Modify: `i18n/locales/{pt-BR,en}.json`
- Test: `test/unit/{live-buffer, incident-stats, findings, finding-keys, sensor-status, chart-rows, event-keys, format}.spec.ts`, `test/unit/useIncidents.spec.ts` (cliente fake como em `useAppVersion.spec.ts`)

**Interfaces (Produces):**
- Tipos: aliases dos gerados em `app/api/types.ts` (`type FindingCode = components['schemas']['FindingResponse']['code']`, `IncidentSummary`, `IncidentDetail`, `LiveResponse`, `HistoryResponse`, `SensorResponse`, …). Nenhum tipo de API escrito à mão.
- `live-buffer.ts`: `createLiveBuffer(snapshot: LiveResponse, windowMs = 600_000)`, `appendFrames(buffer, event): LiveBuffer` (descarta quadros mais antigos que a janela e repetidos), `seriesOf(buffer, sensorKey): {t: number, v: number | null}[]`, `latestValue(buffer, sensorKey): number | null`.
- `useLive()`: `GET /api/live` → buffer; `EventSource('/api/live/stream?segmentId=…&after=…')`; `frames` → `appendFrames`; `segment` → refaz o GET; `state` → atualiza `state`; ao reconectar, refaz o GET; fecha o `EventSource` no `onScopeDispose`. Expõe `{ state, session, sensors, buffer, error }`.
- `incident-stats.ts`: `summarize(incidents, now): { total, last24h, earlyBoot, duringSleep, perDay: { day: string, count: number }[] }` (`earlyBoot` = tem observação `boot.early_failure`; `duringSleep` = `sleepState !== 0`; `perDay` do primeiro dia com queda até hoje, dias sem queda com 0, dia no fuso local).
- `finding-keys.ts`: `export const findingKeys = { 'rail.out_of_tolerance': 'findings.rail_out_of_tolerance', … } as const satisfies Record<FindingCode, string>` (chave i18n = código com `.` → `_`). Um código novo no C# quebra o `typecheck` até ganhar texto.
- `findings.ts`: `groupFindings(findings): GroupedFinding[]` (mesmo código + parâmetros → um item com a lista de hipóteses, como o painel promete em diagnosis.md); `findingParams(code, parameters, locale)` (números invariantes `"11.3"` → `"11,3"` no pt-BR; `rail` `"12V"` → `"+12 V"`; `role` → nome traduzido do papel; `state` 3/4 → suspensão/hibernação; `sources` do WHEA → lista legível).
- `sensor-status.ts`: `statusOf(value: number | null, sensor: SensorResponse): 'success' | 'warning' | 'error' | 'neutral'` (sem faixa e sem limite → `neutral`; rail dentro → `success`, fora → `error`; CPU ≥ `critical` → `error`, ≥ `limit` → `warning`, abaixo → `success`; `null` → `neutral`).
- `chart-rows.ts`: `toChartRows(series: {t, v}[][], keys: string[], expectedStepMs): Row[]` com rótulo único por ponto e uma linha vazia (valores `undefined`) onde `Δt > 3 × expectedStepMs`, para a linha do `UChart` se interromper.
- `event-keys.ts`: `eventKey(provider, eventId): string` (41, 6005, 6006, 6008, 1074, 1001, WHEA 1/17/18/19/20/46/47 → `events.<provider>_<id>`; qualquer outro → `events.unknown` com provider/id).
- `useStorageState()`: lê `storage` de `/api/health`; páginas mostram `SharedStorageNotice` quando não é `Ready`.

- [ ] **Step 1: Testes que falham** (Vitest, ambiente node)

```ts
it('drops frames older than the window and duplicates')            // live-buffer
it('returns null for a sensor without a reading')                   // valor null no quadro → latestValue null
it('counts early boot, sleep and the last 24 hours')                // incident-stats com 4 incidentes de fixture
it('fills days without incidents with zero')                        // 16/11 e 18/11 com quedas → 17/11 = 0
it('groups the same finding for two hypotheses into one item')      // rail.out_of_tolerance PSU + BPD → 1 item, hypotheses [PowerSupply, BoardPowerDelivery]
it('formats invariant numbers in the user locale')                  // findingParams('rail.out_of_tolerance', {observed:'11.28',...}, 'pt-BR').observed == '11,28'
it('has a message for every finding code in both locales')          // Object.values(findingKeys) ⊂ chaves de pt-BR e de en
it('classifies rails and cpu by the ranges sent by the api')        // 11.3 com range 11.4–12.6 → error; 96 °C com limit 95 → warning; 101 → error; sem range → neutral
it('breaks the line where the PC was off')                          // pontos a cada 1 s com um buraco de 30 s → 1 linha vazia no meio, rótulos únicos
it('names known windows events and falls back for unknown ones')
```

- [ ] **Step 2:** `corepack pnpm test` → FAIL.
- [ ] **Step 3:** implementar; textos em pt-BR e en para estados, hipóteses, confiança, códigos, papéis, eventos e avisos de armazenamento.
- [ ] **Step 4:** `corepack pnpm test`, `corepack pnpm lint`, `corepack pnpm typecheck` → PASS.
- [ ] **Step 5:** commit `feat: add the dashboard composables and presentation helpers`.

---

### Task 9: Página Ao vivo

**Files:**
- Create: `app/components/shared/{MetricCard.vue, StatCard.vue, ConfidenceBadge.vue, TimeSeriesChart.vue, StorageNotice.vue}`
- Create: `app/components/live/{LiveStatus.vue, LiveMetrics.vue, RailGauges.vue, RailGauge.vue, LiveTrendChart.vue, OtherSensors.vue, RecentIncidents.vue}`
- Modify: `app/pages/index.vue` (só composição)
- Test: `test/nuxt/{RailGauge, MetricCard}.spec.ts` (`// @vitest-environment nuxt`, `mountSuspended`)

Conteúdo, como no mockup aprovado: indicador "Gravando 1×/s · há Xh" (verde pulsando em `Recording`, âmbar em `Stale` com "sem leituras há X s", cinza em `Stopped`/`NoData`); cards de CPU (°C, com o limite), +12 V (com a faixa), consumo e carga com minigráfico de 10 min; gráfico de temperatura, carga e consumo; régua das tensões com a faixa ATX; Vcore, ventoinhas e temperatura das memórias pelos nomes gravados; últimas 4 quedas com link para o detalhe. `TimeSeriesChart` envolve o `UChart` usando `toChartRows` (rótulos únicos, linhas interrompidas) e é o único lugar que monta dados de gráfico.

- [ ] **Step 1: Testes que falham**

```ts
it('places the marker inside the band for an in-range rail')     // RailGauge 12.19 em 11.4–12.6 → posição entre 10% e 90%, cor success
it('shows a dash and neutral status without a reading')          // MetricCard value null → "—", badge neutral
```

- [ ] **Step 2:** `corepack pnpm test` → FAIL.
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** `corepack pnpm test`, `lint`, `typecheck` → PASS; `corepack pnpm dev` com o Dashboard rodando localmente (`dotnet run --project src/PcMonitor.Dashboard` com `Dashboard__DatabasePath` apontando para uma cópia do banco feita como admin) e conferir no painel do navegador.
- [ ] **Step 5:** commit `feat: build the live page`.

---

### Task 10: Páginas de incidentes

**Files:**
- Create: `app/components/incidents/{IncidentStats.vue, IncidentsPerDayChart.vue, IncidentTable.vue, IncidentHeader.vue, HypothesisRanking.vue, IncidentTimeline.vue, FailureTelemetry.vue, FindingList.vue, LimitationList.vue}`
- Modify: `app/pages/incidents/index.vue`; Create: `app/pages/incidents/[id].vue` (só composição)
- Test: `test/nuxt/{HypothesisRanking, FindingList, IncidentTimeline}.spec.ts`

Conteúdo, como no mockup aprovado: quatro cards (total, últimas 24 h, logo após ligar, durante hibernação); quedas por dia; tabela com horário (`± 1 s (PC Monitor)` quando a fonte é `LastFrame`, "aproximado (Windows)" caso contrário), causa provável + `ConfidenceBadge` ("Sem conclusão" quando `Inconclusive` ou sem diagnóstico), selos de telemetria/hibernação/logo após ligar; clique abre o detalhe. Detalhe: título com o horário estimado; causa mais provável e uma frase por nível de confiança (`incident.summary.<confidence>`); placar das hipóteses em barras divergentes; linha do tempo montada com os campos do incidente (último sinal de vida, último quadro, queda, boot seguinte) e `events` (nomes por `eventKey`); os 2 minutos antes da queda (+12 V com a faixa, CPU e carga) quando há telemetria, com aviso quando `atFailure` é falso; "O que pesou" (evidências agrupadas a favor/contra com a força e observações) e "Limites desta análise".

- [ ] **Step 1: Testes que falham**

```ts
it('draws positive scores to the right and negative to the left')  // HypothesisRanking
it('renders a grouped finding once with both hypotheses')         // FindingList: texto traduzido com "11,28", "a favor de Fonte e Placa / alimentação"
it('orders the timeline by time and names event 41')              // IncidentTimeline
```

- [ ] **Step 2:** `corepack pnpm test` → FAIL.
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** `corepack pnpm test`, `lint`, `typecheck` → PASS; conferir as 16 quedas reais no painel do navegador.
- [ ] **Step 5:** commit `feat: build the incident list and detail pages`.

---

### Task 11: Página de histórico

**Files:**
- Create: `app/components/history/{HistoryToolbar.vue, HistoryStats.vue, HistoryChart.vue}`
- Modify: `app/pages/history.vue` (só composição; o placeholder sai)
- Test: `test/nuxt/HistoryStats.spec.ts`

Conteúdo, como no mockup aprovado: seletor de sensor (`/api/history/sensors`, papéis primeiro, depois por nome) e abas 1 hora / 24 horas / 7 dias / 14 dias; mínimo, média e máximo do período; gráfico de máximo/média/mínimo com linhas interrompidas nos buracos; selos com as quedas do período, que levam ao detalhe. Sensor e período ficam na query da URL (`?sensor=…&range=…`), assim um link da tela de incidentes pode abrir o histórico já no sensor certo.

- [ ] **Step 1: Teste que falha**

```ts
it('shows min, mean and max of the buckets in the user locale')  // baldes de fixture → "12,17", "12,19", "12,21"
```

- [ ] **Step 2:** `corepack pnpm test` → FAIL.
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** `corepack pnpm test`, `lint`, `typecheck`, `corepack pnpm generate` → PASS.
- [ ] **Step 5:** commit `feat: build the history page`.

---

### Task 12: Instalação, CI e documentação

**Files:**
- Rename: `scripts/CollectorService.psm1` → `scripts/PcMonitorService.psm1` (`Stop-PcMonitorService -Name`, `Invoke-Sc`, `Get-PcMonitorDataPath`, `Get-PcMonitorInstallPath -Component`); ajustar `Install-Collector.ps1`/`Uninstall-Collector.ps1`
- Create: `scripts/Install-Dashboard.ps1`, `scripts/Uninstall-Dashboard.ps1`
- Modify: `.github/workflows/ci.yml` (+ `pnpm generate` depois dos testes do frontend)
- Create: `docs/adr/0013-charts-with-nuxt-ui-uchart.md` (gráficos com `UChart`/TanStack Charts em vez do Unovis previsto no estudo; motivo: componente nativo do Nuxt UI 4.13, visual coerente, sem segunda biblioteca), `docs/adr/0014-read-api-sse-and-event-privacy.md` (REST + SSE com polling do banco a cada 1 s, portão de armazenamento 503, eventos sem dados por privacidade)
- Modify: `docs/adr/README.md`, `docs/adr/0003-…md` (nome da conta: `NT SERVICE\PcMonitor.Dashboard`), `docs/architecture/{overview.md, flows.md (fluxo ao vivo com SSE e fluxo de abrir um incidente), data-model.md (ACL), frontend.md (páginas, composables, gráficos)}`, `src/PcMonitor.Dashboard/AGENTS.md`, `src/PcMonitor.Dashboard.Web/AGENTS.md` (gráficos só via `shared/TimeSeriesChart`; textos de código pelo `finding-keys.ts`)

`Install-Dashboard.ps1` (admin, mesmo estilo do coletor): exige `artifacts/dashboard` (`dotnet publish src/PcMonitor.Dashboard -c Release -o artifacts/dashboard`, como usuário comum); para o serviço se existir; copia para `%ProgramFiles%\PcMonitor\Dashboard`; registra a fonte `PcMonitor.Dashboard` no Log de Eventos; cria o serviço com `obj= "NT SERVICE\PcMonitor.Dashboard"`, início automático e reinício em falha; obtém o SID com `sc.exe showsid PcMonitor.Dashboard` e concede **leitura e execução** `(OI)(CI)RX` em `%ProgramData%\PcMonitor` (o `icacls /grant:r` do coletor só substitui as entradas de SYSTEM e Administradores, então a do painel sobrevive a uma reinstalação do coletor); cria o atalho "PC Monitor" (`.url` para `http://127.0.0.1:5280`) no menu Iniciar de todos os usuários; inicia e espera `Running`; consulta `/api/health` e imprime o estado do armazenamento. `Uninstall-Dashboard.ps1` remove serviço, binários, atalho e a entrada da ACL; nunca toca nos dados.

- [ ] **Step 1:** `pwsh -NoProfile -Command "Invoke-ScriptAnalyzer scripts -Recurse"` se disponível; senão, `pwsh -NoProfile -Command "[System.Management.Automation.Language.Parser]::ParseFile(...)"` em cada script → sem erros de sintaxe.
- [ ] **Step 2:** `dotnet publish src/PcMonitor.Dashboard -c Release -o artifacts/dashboard` → `artifacts/dashboard/wwwroot/index.html` existe.
- [ ] **Step 3:** `dotnet build PcMonitor.slnx`, `dotnet test --solution PcMonitor.slnx`, `dotnet format PcMonitor.slnx --verify-no-changes`, `pwsh scripts/Test-AgentDocs.ps1`, `corepack pnpm dlx jscpd@5.4.3` → tudo verde.
- [ ] **Step 4:** commit `feat: install the dashboard service and document the read side`.

---

### Task 13: Instalar e verificar nesta máquina

O dono roda, em janela elevada: `Install-Collector.ps1` (se o coletor mudou) e `Install-Dashboard.ps1`. Verificação (Claude, pelo painel do navegador e pelo Log de Eventos):

- `http://127.0.0.1:5280` abre; Ao vivo em `Recording`, valores mudando a cada segundo **com o coletor gravando** (confirma o leitor com `-shm` só leitura sob a conta virtual). Se falhar com erro de leitura: plano B do estudo, conceder escrita só no `-shm` à conta do painel (ADR 0014 registra qual valeu).
- Incidentes: 16 quedas reais, 2 em hibernação, 5 logo após ligar; detalhe de uma delas sem telemetria abre com as limitações.
- Histórico: +12 V nas últimas 24 h com dados até o minuto atual.
- Processo `PcMonitor.Dashboard.exe` rodando como `NT SERVICE\PcMonitor.Dashboard`; `icacls %ProgramData%\PcMonitor` mostra a entrada RX; o painel não escreve nada na pasta (horário de modificação do banco só muda pelo coletor).
- Log de Eventos: fonte `PcMonitor.Dashboard` com o início do serviço, sem erros.

Registrar o resultado na seção "Resultado da instalação" deste plano e fazer o commit `docs: record plan 05 installation results`.

## Fora deste plano

- Playwright e2e: exige baixar navegadores; a cobertura fica com testes de componente (`@nuxt/test-utils`) e a verificação manual da Task 13. Reavaliar na v2.
- Exportar relatório de um incidente (PDF/Markdown).
- Nomear as ventoinhas no perfil da placa (hoje aparecem com os nomes do LibreHardwareMonitor).
- Pendências antigas que continuam adiadas: snapshot dourado da versão do motor, retenção relendo tudo a cada hora, WAL que não encolhe, 405→404 e barra final no Dashboard.

## Critério de pronto

Todas as tasks com testes verdes; CI local verde (build, testes .NET, format, lint, typecheck, testes do frontend, generate, contrato, agent docs, jscpd); painel instalado como serviço sem privilégio mostrando os dados reais desta máquina; revisão final do branch feita por um revisor novo.
