import type { LiveState, SensorRole } from '~/api/types'
import { latestValue, sensorByRole, type LiveBuffer } from '~/utils/live-buffer'
import { statusOf, type SensorStatus } from '~/utils/sensor-status'

/** One number of the always-on-top panel. */
export interface MiniReading {
  readonly role: SensorRole
  readonly labelKey: string
  readonly value: number | null
  readonly digits: number
  readonly status: SensorStatus
}

/** The few readings the panel shows, always in this order (ADR 0018). */
const shown: readonly { role: SensorRole, labelKey: string, digits: number }[] = [
  { role: 'CpuTemperature', labelKey: 'mini.cpu', digits: 1 },
  { role: 'CpuLoad', labelKey: 'mini.load', digits: 0 },
  { role: 'Rail12V', labelKey: 'roles.Rail12V', digits: 2 },
  { role: 'Rail5V', labelKey: 'roles.Rail5V', digits: 2 },
  { role: 'Rail3V3', labelKey: 'roles.Rail3V3', digits: 2 },
]

/** The panel's readings, coloured by the same rule as the dashboard (ranges and limits from the API). */
export function miniReadings(buffer: LiveBuffer | null): MiniReading[] {
  return shown.map(({ role, labelKey, digits }) => {
    const sensor = buffer ? sensorByRole(buffer, role) : undefined
    const value = buffer && sensor ? latestValue(buffer, sensor.key) : null
    return { role, labelKey, digits, value, status: sensor ? statusOf(value, sensor) : 'neutral' }
  })
}

/** Green only while frames arrive and the dashboard answers; red when recording stopped or the dashboard is down. */
export function recordingDot(state: LiveState, problem: string | null): 'success' | 'error' {
  return state === 'Recording' && problem === null ? 'success' : 'error'
}
