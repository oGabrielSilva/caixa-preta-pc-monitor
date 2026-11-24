# Plano 04 — Motor de diagnóstico

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** para cada incidente, produzir um diagnóstico **explicável**: hipóteses ordenadas por pontuação, cada uma com as evidências a favor e contra, observações neutras e as limitações daquele caso. Tudo gravado no banco e reavaliado quando as regras mudarem.

**Architecture:** o diagnóstico é lógica pura no Domain: regras independentes (`IDiagnosticRule`) recebem um `IncidentEvidence` (incidente + janela de telemetria por papel de sensor + eventos relacionados) e devolvem `Finding`s; o `DiagnosisEngine` só soma e ordena (OCP: nova regra = nova classe). A Application monta a entrada e grava a saída por portas; o SQLite constrói a janela de telemetria a partir dos quadros; o coletor diagnostica ao iniciar, logo depois da detecção. As evidências são **códigos com parâmetros**, não texto: o painel (Plano 05) traduz para pt-BR/en.

**Tech Stack:** .NET 10 · Microsoft.Data.Sqlite + Dapper · System.Text.Json · xUnit v3, Shouldly.

**Spec:** [estudo](../estudos/2026-11-24-estudo-arquitetura.md) §6.3 · [ADR 0005](../adr/0005-event-log-as-incident-source-of-truth.md) · [Plano 03](2026-11-24-plano-03-incidentes.md) · [resultado do spike](../estudos/2026-11-24-resultado-spike-sensores.md) (precisão dos rails).

## Fatos verificados nesta máquina (24/11/2026)

- 16 quedas, **0 WHEA, 0 BugCheck 1001**, `PowerButtonTimestamp` = 0 em todas.
- `SleepInProgress` é o **estado de energia** em transição, não um booleano: as duas quedas "em suspensão" têm **4** (S4: hibernação/Fast Startup). Hoje o domínio guarda `bool` e perde o valor.
- O 6008 é escrito ~a cada 40 min a partir do 6005 do boot. Em **5 das 16** quedas o último sinal de vida é o próprio início do log: a queda aconteceu antes do primeiro batimento (< ~40 min após ligar).
- Precisão dos rails (spike): degrau de ~0,1 V no +12 V e ~0,04 V no +5 V; leitura 1×/s.

## Global Constraints

- Tudo dos planos anteriores continua valendo (camadas, CPM, testes antes, inglês no código, português nos docs).
- **Domain puro:** regras, limiares e motor em `src/PcMonitor.Domain/Diagnosis/`, sem I/O.
- Hipóteses: `PowerSupply`, `BoardPowerDelivery` (placa/curto/reset), `Thermal`, `Software`, `CpuOrMemory`, `ForcedPowerOff`.
- Força das evidências: `Weak` = 1, `Moderate` = 2, `Strong` = 3; contra = negativo. Pontuação = soma por hipótese.
- Confiança do topo: `High` se pontuação ≥ 5 **e** vantagem ≥ 3 sobre a segunda; `Medium` se ≥ 3; `Low` se > 0; `Inconclusive` se nenhuma hipótese tem pontuação positiva.
- Janela de telemetria: os **120 s** até `TelemetryEndAt`, em todos os segmentos da sessão; "fim" = últimos **10 s**; `NaN` ignorado.
- Limiares (constantes em `DiagnosisThresholds`, documentados em `docs/architecture/diagnosis.md`):
  - CPU: ≥ 90 °C forte; ≥ 80 °C moderado; máximo < 75 °C contra (moderado); subida ≥ 15 °C nos últimos 30 s fraco.
  - Rails ATX ±5%: 12 V 11,40–12,60 · 5 V 4,75–5,25 · 3,3 V 3,135–3,465.
  - Queda de rail: mínimo dos últimos 10 s ≤ mediana da janela − **0,2 V** (12 V) / **0,1 V** (5 V) — acima do degrau do ADC.
  - Carga: média dos últimos 10 s ≥ 70% alta; ≤ 20% ociosa.
  - Religamento: boot seguinte ≤ **3 min** após o fim da telemetria = religou sozinha; ≥ **15 min** = ficou desligada.
- `DiagnosisEngine.Version` (inteiro) muda sempre que regras/limiares mudam; incidentes com diagnóstico de versão menor são reavaliados no próximo início.
- Eventos relacionados de incidentes antigos (janelas largas, Plano 03) são **recortados** entre os incidentes vizinhos ao montar a entrada.

## Review Focus

1. **Incidente sem telemetria** (os 16 atuais): só regras de evento; diagnóstico `Low`/`Inconclusive` com a limitação `telemetry.missing`, sem exceção. Teste na Task 3.
2. **Sensor de papel ausente** (placa sem perfil, `DriverMissing`): a regra correspondente não emite nada e acrescenta a limitação `sensor.unavailable`. Testes na Task 2.
3. **Janela com poucos quadros** (coletor caiu segundos após iniciar): regras de tendência exigem ≥ 10 quadros; não concluem com 2. Teste na Task 2.
4. **Regras mudam:** reiniciar o coletor com `Version` maior reavalia todos os incidentes; com a mesma versão, nenhum. Testes nas Tasks 3 e 4.
5. **Evento relacionado de outra queda** (janelas antigas largas): não entra na avaliação. Teste na Task 4.

---

### Task 1: Estado de suspensão e modelo de diagnóstico

**Files:**
- Modify: `src/PcMonitor.Domain/Incidents/UnexpectedShutdown.cs` (`bool SleepInProgress` → `int SleepState`, 0 = nenhuma transição; `bool SleepInProgress => SleepState != 0`)
- Modify: `src/PcMonitor.Infrastructure.Windows/EventLog/UnexpectedShutdownMapper.cs`, `src/PcMonitor.Application/Incidents/DetectIncidents.cs`, `src/.../Persistence.Sqlite/Incidents/SqliteIncidentStore.cs` (grava `sleep_in_progress` = estado) e testes afetados
- Create: `src/PcMonitor.Domain/Diagnosis/Hypothesis.cs`, `EvidenceStrength.cs`, `Finding.cs`, `HypothesisScore.cs`, `Confidence.cs`, `Diagnosis.cs`, `IDiagnosticRule.cs`, `DiagnosisEngine.cs`, `TelemetryWindow.cs`, `IncidentEvidence.cs`
- Test: `tests/PcMonitor.Domain.Tests/Diagnosis/DiagnosisEngineTests.cs`, `TelemetryWindowTests.cs`; ajuste em `UnexpectedShutdownTests`

**Interfaces (Produces):**
- `enum Hypothesis { PowerSupply, BoardPowerDelivery, Thermal, Software, CpuOrMemory, ForcedPowerOff }`
- `enum EvidenceStrength { Weak = 1, Moderate = 2, Strong = 3 }`
- `enum FindingKind { Evidence, Observation, Limitation }`
- `sealed record Finding(string Code, FindingKind Kind, Hypothesis? Hypothesis, bool Supports, EvidenceStrength Strength, IReadOnlyDictionary<string, string> Parameters)` — só `Evidence` tem hipótese e pontua: `Weight => Kind == Evidence ? (Supports ? 1 : -1) * (int)Strength : 0`. Fábricas: `Finding.For(code, hypothesis, strength, params?)`, `Finding.Against(...)`, `Finding.Observation(code, params?)`, `Finding.Limitation(code, params?)`. O motor separa as limitações (deduplicadas por código + parâmetros) do resto.
- `sealed record HypothesisScore(Hypothesis Hypothesis, int Score)`; `enum Confidence { Inconclusive, Low, Medium, High }`
- `sealed record Diagnosis(int EngineVersion, IReadOnlyList<HypothesisScore> Ranking, Confidence Confidence, IReadOnlyList<Finding> Findings, IReadOnlyList<Finding> Limitations)` — `Ranking` só com pontuação ≠ 0, decrescente; empate desfeito pela ordem do enum.
- `sealed class TelemetryWindow` — `IReadOnlyList<(DateTimeOffset At, float Value)> Series(SensorRole role)` (vazia se ausente), `DateTimeOffset End`, `bool Has(SensorRole role)`, `IReadOnlyList<(DateTimeOffset, float)> Tail(SensorRole role, TimeSpan span)`; construída com `TelemetryWindow(DateTimeOffset end, IReadOnlyDictionary<SensorRole, IReadOnlyList<(DateTimeOffset, float)>> series)`, descartando `NaN`.
- `sealed record IncidentEvidence(long IncidentId, Incident Incident, TelemetryWindow? Telemetry, IReadOnlyList<SystemEvent> RelatedEvents)`
- `interface IDiagnosticRule { IEnumerable<Finding> Evaluate(IncidentEvidence evidence); }`
- `sealed class DiagnosisEngine(IEnumerable<IDiagnosticRule> rules)` — `public const int Version = 1;` `Diagnosis Diagnose(IncidentEvidence evidence)`; acrescenta sempre a limitação `telemetry.missing` quando `Telemetry is null`, e `estimate.lower_bound` quando a fonte do horário é `LastAliveEventLog` ou `LastFrameBeforeSleep`.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void SleepInProgress_is_true_for_any_transition_state() // SleepState 4 → true; 0 → false
[Fact] public void Engine_sums_weights_per_hypothesis_and_ranks_descending()   // For Strong PSU (+3), Against Weak PSU (−1), For Moderate Thermal (+2) → [PSU 2, Thermal 2] ordem do enum
[Fact] public void Observations_do_not_score()
[Theory] // top, segundo → confiança
[InlineData(6, 2, Confidence.High)] [InlineData(5, 3, Confidence.Medium)] [InlineData(3, 0, Confidence.Medium)]
[InlineData(1, 0, Confidence.Low)] [InlineData(0, 0, Confidence.Inconclusive)]
public void Confidence_follows_score_and_margin(int top, int second, Confidence expected)
[Fact] public void Missing_telemetry_is_always_a_limitation()
[Fact] public void Lower_bound_failure_time_is_a_limitation()
[Fact] public void Telemetry_window_drops_NaN_and_returns_the_tail()
```

- [ ] **Step 2–4:** falhar → implementar → passar (suíte toda, pois `SleepState` mexe em 3 projetos).
- [ ] **Step 5: Commit** — `feat: add diagnosis model, engine and sleep state`

---

### Task 2: Regras

**Files:** `src/PcMonitor.Domain/Diagnosis/DiagnosisThresholds.cs`, `RailTolerance.cs`, `Rules/ThermalRule.cs`, `RailToleranceRule.cs`, `RailDroopRule.cs`, `LoadAtFailureRule.cs`, `RestartGapRule.cs`, `SleepTransitionRule.cs`, `BugcheckRule.cs`, `WheaRule.cs`, `PowerButtonRule.cs`, `BootTimingRule.cs`, `DiagnosticRules.cs` (`All` = lista de todas)
**Test:** `tests/PcMonitor.Domain.Tests/Diagnosis/Rules/*Tests.cs`, `Support/EvidenceBuilder.cs` (monta `IncidentEvidence` com séries por papel e eventos)

**Regras (código → efeito):**

| Regra | Condição | Findings |
|---|---|---|
| `ThermalRule` | requer `CpuTemperature` | máx ≥ 90 → `thermal.critical` For Thermal Strong · 80–90 → `thermal.high` For Moderate · máx < 75 → `thermal.normal` Against Moderate · subida ≥ 15 °C nos últimos 30 s (≥ 10 quadros) → `thermal.rising` For Weak |
| `RailToleranceRule` | por rail com papel | amostra fora da faixa → `rail.out_of_tolerance {rail,min,max,observed}` For PowerSupply Strong · todos dentro → `rail.in_tolerance {rail}` Against PowerSupply Weak + limitação `rail.precision` |
| `RailDroopRule` | 12 V/5 V, ≥ 10 quadros | mínimo dos últimos 10 s ≤ mediana − limiar → `rail.droop {rail,median,min}` For PowerSupply Moderate |
| `LoadAtFailureRule` | requer `CpuLoad` (≥ 5 quadros nos últimos 10 s) | ≥ 70% → `load.high {avg}` For PowerSupply Weak + For Thermal Weak · ≤ 20% → `load.idle {avg}` For BoardPowerDelivery Moderate + Against PowerSupply Weak |
| `RestartGapRule` | fonte do horário = `LastFrame` | `LoggedAt − TelemetryEnd` ≤ 3 min → `restart.automatic {seconds}` For BoardPowerDelivery Moderate + Against PowerSupply Weak · ≥ 15 min → `restart.stayed_off {minutes}` For PowerSupply Weak |
| `SleepTransitionRule` | `SleepState ≠ 0` | `power.transition {state}` For BoardPowerDelivery Moderate + For PowerSupply Weak |
| `BugcheckRule` | — | código 0x124 → `bugcheck.whea {code}` For CpuOrMemory Strong · outro ≠ 0 → `bugcheck {code}` For Software Strong · 0 → `bugcheck.none` Against Software Weak |
| `WheaRule` | — | evento WHEA relacionado → `whea.event {id}` For CpuOrMemory Strong · `WHEABootErrorCount` > 0 → `whea.boot_errors {count}` For CpuOrMemory Moderate |
| `PowerButtonRule` | — | `LongPowerButtonPress` → `power_button.long_press` For ForcedPowerOff Strong · `PowerButtonPressedAt` ≠ null → `power_button.pressed` For ForcedPowerOff Moderate |
| `BootTimingRule` | evento 6005 relacionado com horário = `LastAliveAt` (± 2 s) | `boot.early_failure` Observation (queda antes do primeiro sinal de vida após ligar) |

Regras que dependem de um papel ausente emitem a limitação `sensor.unavailable {role}` (Review Focus 2).

- [ ] **Step 1: Testes que falham** — por regra, um teste por linha da tabela (condição → findings exatos, com códigos e parâmetros), mais:

```csharp
[Fact] public void Thermal_rule_reports_the_missing_sensor_as_a_limitation()        // Review Focus 2
[Fact] public void Droop_rule_does_not_conclude_with_fewer_than_ten_frames()         // Review Focus 3
[Fact] public void Restart_gap_rule_ignores_lower_bound_failure_times()
[Fact] public void Every_rule_is_registered_in_DiagnosticRules_All()
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: add diagnostic rules`

---

### Task 3: Caso de uso `DiagnoseIncidents`

**Files:** `src/PcMonitor.Application/Diagnosis/IDiagnosisInputReader.cs`, `IDiagnosisWriter.cs`, `DiagnoseIncidents.cs`; **Test:** `tests/PcMonitor.Application.Tests/Diagnosis/DiagnoseIncidentsTests.cs` (+ fakes)

**Interfaces:**
- `interface IDiagnosisInputReader { IReadOnlyList<IncidentEvidence> IncidentsNeedingDiagnosis(int engineVersion); }` — incidentes sem diagnóstico ou com versão menor.
- `interface IDiagnosisWriter { void Save(long incidentId, Diagnosis diagnosis); }`
- `sealed partial class DiagnoseIncidents(IDiagnosisInputReader reader, IDiagnosisWriter writer, DiagnosisEngine engine, ILogger<DiagnoseIncidents> logger)` — `int Run()`: diagnostica e grava cada um; falha por incidente é registrada (EventId 301) e não interrompe os outros; log Information (EventId 300) com a hipótese do topo e a confiança.

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Each_incident_needing_diagnosis_is_diagnosed_and_saved()
[Fact] public void Reader_is_asked_for_the_current_engine_version()                 // Review Focus 4
[Fact] public void Incident_without_telemetry_gets_a_diagnosis_with_the_missing_telemetry_limitation() // Review Focus 1
[Fact] public void A_failing_incident_does_not_stop_the_others()
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: diagnose incidents`

---

### Task 4: Persistência do diagnóstico e montagem da entrada

**Files:**
- Create: `src/.../Persistence.Sqlite/Migrations/005_diagnosis.sql`
- Create: `src/.../Persistence.Sqlite/Diagnosis/SqliteDiagnosisStore.cs` (implementa as duas portas), `Diagnosis/DiagnosisSql.cs`, `Diagnosis/DiagnosisJson.cs` (serialização dos findings)
- Test: `tests/.../SqliteDiagnosisStoreTests.cs`; `SchemaMigratorTests` (`LatestVersion = 5`)

**`005_diagnosis.sql`:**

```sql
CREATE TABLE diagnosis (
    incident_id INTEGER PRIMARY KEY REFERENCES incident (id),
    engine_version INTEGER NOT NULL, diagnosed_at_ms INTEGER NOT NULL,
    top_hypothesis TEXT, confidence TEXT NOT NULL,
    ranking_json TEXT NOT NULL, findings_json TEXT NOT NULL, limitations_json TEXT NOT NULL);
-- Sleep state was stored as 0/1; recover the real state (e.g. 4 = hibernate/Fast Startup) from the event data.
UPDATE incident SET sleep_in_progress = CAST(json_extract(data_json, '$.SleepInProgress') AS INTEGER)
WHERE json_extract(data_json, '$.SleepInProgress') IS NOT NULL;
```

**Montagem da entrada (`IncidentsNeedingDiagnosis`):**
- `Incident` reconstruído das colunas (`data_json` → `Data`); `Shutdown.SleepState` = `sleep_in_progress`.
- `Telemetry`: se `telemetry_end_ms` e `session_id`, quadros da sessão (todos os segmentos) com `ts_ms` em `[end − 120 s, end]`, decodificados com o catálogo de **cada** segmento e agrupados por `SensorRole` (papéis `None` descartados); senão `null`.
- `RelatedEvents`: `incident_event` do incidente, **recortados** ao intervalo aberto entre o `logged_at_ms` do incidente anterior e o do seguinte (Review Focus 5).
- `Save`: `INSERT … ON CONFLICT (incident_id) DO UPDATE` (substitui versão antiga); `diagnosed_at_ms` = agora (via `TimeProvider`).

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public void Incident_without_diagnosis_needs_one()
[Fact] public void Diagnosis_of_the_current_version_is_not_redone_but_an_older_one_is()      // Review Focus 4
[Fact] public void Telemetry_window_has_the_last_120_seconds_by_role_across_segments()
[Fact] public void Telemetry_is_null_for_an_incident_without_session()
[Fact] public void Related_events_outside_the_neighbouring_incidents_are_left_out()          // Review Focus 5
[Fact] public void Save_replaces_an_older_diagnosis()
[Fact] public void Migration_5_recovers_the_sleep_state_from_the_event_data()                // 1 → 4
```

- [ ] **Step 2–4:** falhar → implementar → passar.
- [ ] **Step 5: Commit** — `feat: persist diagnoses and build diagnosis input from telemetry`

---

### Task 5: Coletor, documentação

**Files:**
- Modify: `CollectorServiceCollectionExtensions.cs` (registra regras via `DiagnosticRules.All`, `DiagnosisEngine`, portas, `DiagnoseIncidents`), `StartupIncidentDetection.cs` → passa a rodar detecção **e** diagnóstico, cada um isolado (falha em um não impede o outro nem a amostragem); renomear para `StartupAnalysis`
- Create: `docs/architecture/diagnosis.md` (hipóteses, tabela de regras e códigos com parâmetros, limiares e por quê, confiança, limitações); `docs/adr/0012-explainable-versioned-diagnosis.md`
- Modify: `flows.md` (motor ✅), `data-model.md` (tabela `diagnosis`), AGENTS de Domain/Application/Persistence/Collector
- Test: `tests/PcMonitor.Collector.Tests/Hosting/StartupAnalysisTests.cs` (diagnóstico roda mesmo se a detecção falhar; registro dos serviços)

- [ ] **Step 1–4:** testes → implementar → passar; suíte, format, agent docs, jscpd; teste local sem administrador num banco temporário (16 diagnósticos; segunda execução: 0 rediagnosticados).
- [ ] **Step 5: Commit** — `feat: diagnose incidents when the collector starts`

---

### Task 6: Reinstalar e verificar nesta máquina

- [ ] **Step 1:** publicar; o script de verificação passa a mostrar, por incidente, hipótese do topo, confiança e códigos das evidências.
- [ ] **Step 2:** o **usuário** reinstala como administrador.
- [ ] **Step 3: Esperado (Log de Eventos):** 16 diagnósticos na primeira execução, 0 na segunda; as duas quedas com `SleepState = 4` com `power.transition` e `BoardPowerDelivery` no topo; as 5 quedas logo após o boot com `boot.early_failure`; todas com `telemetry.missing` e confiança `Low`/`Inconclusive`.
- [ ] **Step 4:** registrar o resultado neste plano e commitar.

## Critério de pronto

- Todo incidente tem um diagnóstico da versão atual, com evidências e limitações explicáveis por códigos.
- A próxima queda real com telemetria recebe regras térmicas, de rails, carga e religamento.
