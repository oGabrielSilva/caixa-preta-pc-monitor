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

Janela: os 120 s até o fim da telemetria, em todos os segmentos da sessão; "fim" = últimos 10 s.

| Código | Tipo | Hipótese / efeito | Quando | Parâmetros |
|---|---|---|---|---|
| `thermal.critical` | Evidência | Thermal +3 | CPU ≥ 90 °C | `max` |
| `thermal.high` | Evidência | Thermal +2 | CPU ≥ 80 °C | `max` |
| `thermal.normal` | Evidência | Thermal −2 | máximo < 75 °C | `max` |
| `thermal.rising` | Evidência | Thermal +1 | subida ≥ 15 °C nos últimos 30 s (≥ 10 quadros) | `delta` |
| `rail.out_of_tolerance` | Evidência | PowerSupply +3 | rail fora da faixa ATX ±5 % | `rail`, `min`, `max`, `observed` |
| `rail.in_tolerance` | Evidência | PowerSupply −1 | todos os rails medidos dentro da faixa | `rails` |
| `rail.droop` | Evidência | PowerSupply +2 | mínimo dos últimos 10 s ≤ mediana − 0,2 V (12 V) / 0,1 V (5 V), ≥ 10 quadros | `rail`, `median`, `min` |
| `load.high` | Evidência | PowerSupply +1, Thermal +1 | carga média dos últimos 10 s ≥ 70 % | `avg` |
| `load.idle` | Evidência | BoardPowerDelivery +2, PowerSupply −1 | carga média dos últimos 10 s ≤ 20 % | `avg` |
| `restart.automatic` | Evidência | BoardPowerDelivery +2, PowerSupply −1 | boot seguinte ≤ 3 min após o último quadro (só com horário `LastFrame`) | `seconds` |
| `restart.stayed_off` | Evidência | PowerSupply +1 | boot seguinte ≥ 15 min após o último quadro (só com `LastFrame`) | `minutes` |
| `power.transition` | Evidência | BoardPowerDelivery +2, PowerSupply +1 | queda durante suspensão/hibernação/Fast Startup | `state` (3 suspensão, 4 hibernação) |
| `bugcheck` | Evidência | Software +3 | tela azul registrada | `code` |
| `bugcheck.whea` | Evidência | CpuOrMemory +3 | tela azul 0x124 | `code` |
| `bugcheck.none` | Evidência | Software −1 | sem tela azul | — |
| `whea.event` | Evidência | CpuOrMemory +3 | evento WHEA perto da queda | `count`, `ids` |
| `whea.boot_errors` | Evidência | CpuOrMemory +2 | `WHEABootErrorCount` > 0 no evento 41 | `count` |
| `power_button.long_press` | Evidência | ForcedPowerOff +3 | botão segurado | — |
| `power_button.pressed` | Evidência | ForcedPowerOff +1 | botão pressionado em algum momento | — |
| `boot.early_failure` | Observação | — | o último sinal de vida é o início do log do boot: caiu antes do primeiro batimento (~40 min) | — |
| `telemetry.missing` | Limitação | — | sem dados do coletor para a queda | — |
| `estimate.lower_bound` | Limitação | — | horário estimado é um limite inferior (6008 ou antes da suspensão) | — |
| `sensor.unavailable` | Limitação | — | a placa/sessão não tem o sensor que a regra precisa | `role` |
| `rail.precision` | Limitação | — | 1 leitura/s e degrau de ~0,1 V no 12 V: quedas rápidas ou pequenas não aparecem | — |

## Por que esses limiares

- **Temperatura:** o Ryzen 8700G limita por volta de 95 °C e desliga logo acima; 80–90 °C já é anormal em repouso.
- **Rails:** faixa ATX ±5 %. As quedas exigem mais que o degrau do ADC após o divisor (~0,1 V no 12 V, ~0,04 V no 5 V), para um degrau de leitura nunca parecer queda.
- **Carga:** fonte e refrigeração falham sob carga; cair parado aponta para a placa.
- **Religamento:** uma proteção da fonte desarmada mantém a máquina desligada; voltar sozinha em minutos parece reset (placa/linha de reset). Só vale quando o último quadro é o instante da queda.
- **Ausências não inocentam:** sem tela azul é evidência fraca contra software (corte, reset e desligamento térmico também não geram tela azul); sem WHEA não pesa nada (um corte apaga os registros).
