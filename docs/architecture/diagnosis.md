# Diagnóstico

Status: ✅ (Plano 04). Decisão: [ADR 0012](../adr/0012-explainable-versioned-diagnosis.md). Código: `src/PcMonitor.Domain/Diagnosis/`.

O diagnóstico **não afirma uma causa**: ordena hipóteses pela soma das evidências e mostra tudo o que pesou, a favor e contra, junto com as limitações do caso. Ele é recalculado sempre que as regras mudam (`DiagnosisEngine.Version`).

## Hipóteses

| Código | Significado |
|---|---|
| `PowerSupply` | A fonte deixou de entregar energia estável (proteção, queda, unidade falhando). |
| `BoardPowerDelivery` | Alimentação ou reset da placa-mãe: VRM, trilhas danificadas, curto, linha de reset. |
| `Thermal` | O processador superaqueceu e o hardware desligou para se proteger. |
| `Software` | Driver ou sistema operacional travou (tela azul). |
| `CpuOrMemory` | Processador ou memória reportaram erros de hardware (WHEA). |
| `ForcedPowerOff` | Botão de energia segurado: não é defeito. |

## Pontuação e confiança

- Evidência: `Weak` = 1, `Moderate` = 2, `Strong` = 3; contra = negativo. Observações e limitações não pontuam.
- Confiança do topo: `High` (≥ 5 e vantagem ≥ 3 sobre a segunda) · `Medium` (≥ 3) · `Low` (> 0) · `Inconclusive` (nenhuma positiva).

## Regras e códigos

Regras de telemetria só avaliam quando o horário da queda é o último quadro (`LastFrame`): se o coletor tinha parado antes ou a máquina estava dormindo, os últimos segundos gravados não são o momento da falha e entra a limitação `telemetry.not_at_failure`. Janela: os 120 s até o fim da telemetria, em todos os segmentos da sessão; "no momento da queda" = últimos 10 s.

| Código | Tipo | Hipótese / efeito | Quando | Parâmetros |
|---|---|---|---|---|
| `thermal.critical` | Evidência | Thermal +3 | algum valor ≥ 100 °C nos últimos 10 s | `max` |
| `thermal.at_limit` | Evidência | Thermal +2 | média ≥ 95 °C (Tjmax) nos últimos 10 s | `avg` |
| `thermal.normal` | Evidência | Thermal −2 | máximo < 75 °C nos últimos 10 s | `max` |
| `rail.out_of_tolerance` | Evidência | PowerSupply +2 **e** BoardPowerDelivery +2 | ≥ 2 amostras fora da faixa ATX ±5 %; só o pior rail conta | `rail`, `min`, `max` (limites ATX), `observed`, `samples` |
| `rail.droop` | Evidência | PowerSupply +1 **e** BoardPowerDelivery +1 | mínimo dos últimos 10 s ≤ mediana − 0,2 V (12 V) / 0,1 V (5 V), ainda dentro da faixa, ≥ 10 quadros | `rail`, `median`, `lowest` |
| `rail.in_tolerance` | Observação | — | todos os rails medidos dentro da faixa | `rails` |
| `load.high` | Evidência | PowerSupply +1, Thermal +1 | carga média dos últimos 10 s ≥ 70 % | `avg` |
| `load.idle` | Evidência | BoardPowerDelivery +1 | carga média dos últimos 10 s ≤ 20 % | `avg` |
| `restart.quick` | Observação | — | boot seguinte ≤ 3 min após o último quadro | `seconds` |
| `restart.stayed_off` | Observação | — | boot seguinte ≥ 15 min após o último quadro | `minutes` |
| `power.transition` | Evidência | BoardPowerDelivery +2, PowerSupply +1 | queda durante suspensão/hibernação/Fast Startup | `state` (3 suspensão, 4 hibernação) |
| `bugcheck` | Evidência | Software +3 | tela azul registrada (exceto 0x124) | `code` |
| `bugcheck.none` | Evidência | Software −1 | sem tela azul | — |
| `whea.hardware_error` | Evidência | CpuOrMemory +1 a +3 | **um** fato por incidente, com a força da fonte mais forte: tela azul 0x124 ou evento fatal (1, 18, 20, 46) +3; erros no boot +2; erro corrigido (19, 47) +1 | `sources` (ex.: `bugcheck:0x00000124,event:18,boot_errors:2`) |
| `whea.pcie` | Observação | — | erro PCIe corrigido (17): placa/slot de um dispositivo, não CPU/memória | — |
| `power_button.long_press` | Evidência | ForcedPowerOff +3 | botão segurado | — |
| `power_button.pressed` | Evidência | ForcedPowerOff +1 | botão pressionado em algum momento | — |
| `boot.early_failure` | Observação | — | o último sinal de vida é o início do log do boot: caiu antes do primeiro batimento (~40 min) | — |
| `telemetry.missing` | Limitação | — | sem dados do coletor para a queda | — |
| `telemetry.not_at_failure` | Limitação | — | há telemetria, mas ela termina antes da queda | — |
| `telemetry.too_short` | Limitação | — | o sensor foi gravado, mas não o bastante antes da queda para a regra concluir | `role` |
| `estimate.lower_bound` | Limitação | — | horário estimado é um limite inferior (6008 ou antes da suspensão) | — |
| `sensor.unavailable` | Limitação | — | a placa/sessão não tem o sensor que a regra precisa | `role` |
| `rail.precision` | Limitação | — | 1 leitura/s e degrau de ~0,1 V no 12 V: quedas rápidas ou pequenas não aparecem | — |
| `rail.measured_on_board` | Limitação | — | o sensor fica na placa, depois do conector e das trilhas: não separa fonte de placa/conector | — |
| `whea.may_be_power` | Limitação | — | instabilidade de energia/VRM também gera erros WHEA | — |

Um mesmo código pode aparecer em duas linhas (uma por hipótese), com os mesmos parâmetros; o painel agrupa por código.

## Formato gravado

`diagnosis.findings_json` e `limitations_json` são listas de `{code, kind, hypothesis?, supports?, strength?, parameters}` (enums por nome, parâmetros como texto); `ranking_json` é `[{hypothesis, score}]`. O formato está fixado por teste (`Stored_json_has_the_shape_the_dashboard_reads`).

## Por que esses limiares

- **Temperatura:** o Ryzen 7 8700G tem Tjmax de 95 °C e, sob carga contínua, trabalha nesse limite por projeto. Por isso só conta o que acontece **no momento da queda**: sentado em 95 °C (moderado) ou acima de 100 °C, zona de desligamento térmico (forte). Um pico antigo na janela não conta.
- **Rails:** faixa ATX ±5 %. Uma amostra isolada fora da faixa pode ser ruído (1 leitura/s), então são exigidas duas. O sensor fica na placa, depois do conector de 24 pinos e das trilhas: tensão errada pesa igual para fonte e placa. As quedas exigem mais que o degrau do ADC após o divisor (~0,096 V no 12 V; na prática 3 degraus, ~0,29 V; ~0,12 V no 5 V).
- **Carga:** fonte e refrigeração costumam falhar sob carga; cair parado inclina levemente para a placa, sem descartar a fonte.
- **Religamento:** o log não distingue "voltou sozinha" de "alguém apertou o botão", e uma fonte fraca também pode reiniciar a máquina; por isso o tempo até o boot seguinte é só uma observação.
- **Eventos de cada queda:** um evento pertence a uma única queda: a que vem depois do boot anterior estabilizado, até 2 min após o 41 dela; o início do log (6005), escrito logo depois do 41 no boot, marca a sessão que a **próxima** queda encerra (`RelatedEventScope`).
- **Ausências não inocentam:** sem tela azul é evidência fraca contra software (corte, reset e desligamento térmico também não geram tela azul); sem WHEA não pesa nada (um corte apaga os registros).
