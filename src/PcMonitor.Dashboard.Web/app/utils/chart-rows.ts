import type { SeriesPoint } from '~/utils/live-buffer'

/** A row for `UChart`: a unique `label` (its x index), the time and one value per series. */
export interface ChartRow {
  readonly label: string
  readonly t: number
  readonly [series: string]: string | number | undefined
}

/**
 * Merges series by time into chart rows. `UChart` places rows on a point scale by label, so labels are made unique,
 * and a row without values is inserted where consecutive points are more than `gapSteps` steps apart, so the line
 * breaks where nothing was recorded (PC off, collector stopped). Raw frames jitter, so they allow three steps; buckets
 * are regular, so one missing bucket is already a gap.
 */
export function toChartRows(
  series: Readonly<Record<string, readonly SeriesPoint[]>>,
  expectedStepMs: number,
  format: (t: number) => string,
  gapSteps = 3,
): ChartRow[] {
  const keys = Object.keys(series)
  const byTime = new Map<number, Record<string, number | undefined>>()
  for (const key of keys) {
    for (const point of series[key] ?? []) {
      const values = byTime.get(point.t) ?? Object.fromEntries(keys.map(k => [k, undefined]))
      values[key] = point.v ?? undefined
      byTime.set(point.t, values)
    }
  }

  const times = [...byTime.keys()].sort((a, b) => a - b)
  const rows: ChartRow[] = []
  const used = new Set<string>()
  const unique = (label: string) => {
    let candidate = label
    while (used.has(candidate)) {
      candidate += '\u200B'
    }
    used.add(candidate)
    return candidate
  }

  times.forEach((t, i) => {
    const previous = times[i - 1]
    if (previous !== undefined && t - previous > gapSteps * expectedStepMs) {
      const middle = (previous + t) / 2
      rows.push({ label: unique(format(middle)), t: middle, ...Object.fromEntries(keys.map(k => [k, undefined])) })
    }
    rows.push({ label: unique(format(t)), t, ...byTime.get(t) })
  })
  return rows
}
