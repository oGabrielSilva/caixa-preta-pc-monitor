import type { HistoryRange, SensorResponse } from '~/api/types'

export const historyRanges: readonly HistoryRange[] = ['Hour', 'Day', 'Week', 'TwoWeeks']

/** Reads the period from the URL query; anything unknown is a day. */
export function parseRange(value: unknown): HistoryRange {
  const first = Array.isArray(value) ? value[0] : value
  return historyRanges.find(range => range === first) ?? 'Day'
}

/**
 * The sensor to show: the one in the URL when it was recorded, otherwise the first one (an old link to a sensor that no
 * longer exists opens the history instead of an error). Before the list arrives, the URL is kept as it is.
 */
export function resolveSensor(requested: string | undefined, sensors: readonly SensorResponse[]): string | undefined {
  if (sensors.length === 0) {
    return requested
  }
  return sensors.some(s => s.key === requested) ? requested : sensors[0]!.key
}
