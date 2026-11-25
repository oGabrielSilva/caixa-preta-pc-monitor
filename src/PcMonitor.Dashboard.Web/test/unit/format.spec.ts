import { describe, expect, it } from 'vitest'
import { formatDay, formatDuration, formatNumber, minutesBetween } from '~/utils/format'

describe('format', () => {
  it('formats numbers in the locale and a dash without a reading', () => {
    expect(formatNumber(12.192, 'pt-BR', 2)).toBe('12,19')
    expect(formatNumber(null, 'pt-BR', 2)).toBe('—')
  })

  it('formats durations compactly', () => {
    expect(formatDuration(74 * 60_000)).toBe('1h14')
    expect(formatDuration(25 * 60_000)).toBe('25 min')
    expect(formatDuration(42_000)).toBe('42 s')
    expect(formatDuration(-5)).toBe('0 s')
  })

  it('counts whole minutes between two times', () => {
    expect(minutesBetween('2026-11-24T17:00:00Z', '2026-11-24T17:14:59Z')).toBe(14)
  })
  it('adds the year to a day only when asked', () => {
    expect(formatDay('2026-11-24', 'pt-BR')).toBe('24/11')
    expect(formatDay('2026-11-24', 'pt-BR', true)).toBe('24/11/26')
  })
})
