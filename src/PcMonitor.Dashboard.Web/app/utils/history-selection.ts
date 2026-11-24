import type { HistoryRange } from '~/api/types'

export const historyRanges: readonly HistoryRange[] = ['Hour', 'Day', 'Week', 'TwoWeeks']

/** Reads the period from the URL query; anything unknown is a day. */
export function parseRange(value: unknown): HistoryRange {
  const first = Array.isArray(value) ? value[0] : value
  return historyRanges.find(range => range === first) ?? 'Day'
}
