/** Display formatting shared by every page. All functions are pure and take the locale explicitly. */

export const noReading = '—'

export function formatNumber(value: number | null | undefined, locale: string, digits = 1): string {
  return value === null || value === undefined
    ? noReading
    : value.toLocaleString(locale, { minimumFractionDigits: digits, maximumFractionDigits: digits })
}

/** Decimal places that make sense for a kind of reading. */
export function digitsFor(kind: 'Temperature' | 'Voltage' | 'Power' | 'Load' | 'Fan'): number {
  switch (kind) {
    case 'Voltage': return 2
    case 'Fan':
    case 'Load': return 0
    default: return 1
  }
}

/** `1h14`, `25 min`, `42 s`. */
export function formatDuration(ms: number): string {
  const seconds = Math.max(0, Math.floor(ms / 1000))
  if (seconds < 60) {
    return `${seconds} s`
  }
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) {
    return `${minutes} min`
  }
  return `${Math.floor(minutes / 60)}h${String(minutes % 60).padStart(2, '0')}`
}

export function minutesBetween(from: string | number, to: string | number): number {
  return Math.floor((new Date(to).getTime() - new Date(from).getTime()) / 60_000)
}

/** `17:56:41` in the local time zone. */
export function formatTime(time: string | number, locale: string): string {
  return new Date(time).toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

/** `24/11, 17:56` in the local time zone. */
export function formatDateTime(time: string | number, locale: string): string {
  return new Date(time).toLocaleString(locale, { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
}

/** `24/11 às 17:56:41` style: date and time with seconds. */
export function formatFullDateTime(time: string | number, locale: string): string {
  return new Date(time).toLocaleString(locale, { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

/** A local day `YYYY-MM-DD` as `24/11` (or `24/11/26` with the year). */
export function formatDay(day: string, locale: string, withYear = false): string {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(year!, month! - 1, date!).toLocaleDateString(locale, withYear
    ? { day: '2-digit', month: '2-digit', year: '2-digit' }
    : { day: '2-digit', month: '2-digit' })
}
