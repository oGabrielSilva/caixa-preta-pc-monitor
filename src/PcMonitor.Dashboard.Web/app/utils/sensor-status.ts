import type { SensorResponse } from '~/api/types'

/** Semantic colour of a reading (see app.config.ts): judged only by the range and limits the API sends. */
export type SensorStatus = 'success' | 'warning' | 'error' | 'neutral'

export function statusOf(value: number | null | undefined, sensor: SensorResponse): SensorStatus {
  if (value === null || value === undefined) {
    return 'neutral'
  }
  if (sensor.range) {
    return value >= sensor.range.min && value <= sensor.range.max ? 'success' : 'error'
  }
  if (sensor.critical !== null && value >= sensor.critical) {
    return 'error'
  }
  if (sensor.limit !== null) {
    return value >= sensor.limit ? 'warning' : 'success'
  }
  return 'neutral'
}
