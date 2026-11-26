import type { RangeResponse } from '~/api/types'
import type { SeriesPoint } from '~/utils/live-buffer'

const width = 500
const height = 150
const pad = 6

/** A small vector line chart of one series for the PDF report, with the safe range as a band when there is one. */
export function telemetrySvg(points: readonly SeriesPoint[], range: RangeResponse | null): string {
  const values = points.flatMap(p => (p.v === null ? [] : [p.v]))
  const low = Math.min(...values, range?.min ?? Number.POSITIVE_INFINITY)
  const high = Math.max(...values, range?.max ?? Number.NEGATIVE_INFINITY)
  const first = points[0]?.t ?? 0
  const span = Math.max(1, (points.at(-1)?.t ?? 1) - first)
  const x = (t: number) => pad + ((t - first) / span) * (width - 2 * pad)
  const y = (v: number) => height - pad - ((v - low) / Math.max(1e-9, high - low)) * (height - 2 * pad)
  const line = points.flatMap(p => (p.v === null ? [] : [`${x(p.t).toFixed(1)},${y(p.v).toFixed(1)}`])).join(' ')
  const band = range ? `<rect x="${pad}" y="${y(range.max).toFixed(1)}" width="${width - 2 * pad}" height="${(y(range.min) - y(range.max)).toFixed(1)}" fill="#10b981" fill-opacity="0.12"/>` : ''
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">${band}<polyline points="${line}" fill="none" stroke="#e11d48" stroke-width="1.5"/></svg>`
}
