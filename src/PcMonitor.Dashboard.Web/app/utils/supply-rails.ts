import type { SensorResponse } from '~/api/types'

const railOrder: readonly SensorResponse['role'][] = ['Rail12V', 'Rail5V', 'Rail3V3']

/**
 * The power supply voltages shown as gauges, in the order +12 V, +5 V, +3.3 V. Chosen by role, not by having a safe
 * range: other sensors have one too (the NVMe life left, 10–100 %) and are not supply voltages.
 */
export function supplyRails(sensors: readonly SensorResponse[]): SensorResponse[] {
  return sensors
    .filter(sensor => railOrder.includes(sensor.role) && sensor.range !== null)
    .sort((a, b) => railOrder.indexOf(a.role) - railOrder.indexOf(b.role))
}
