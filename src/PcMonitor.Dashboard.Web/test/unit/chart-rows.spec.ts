import { describe, expect, it } from 'vitest'
import { toChartRows } from '~/utils/chart-rows'

const t0 = Date.UTC(2026, 10, 24, 17, 0, 0)
const label = (t: number) => String((t - t0) / 1000)

describe('toChartRows', () => {
  it('merges series by time with one unique label per row', () => {
    const rows = toChartRows({ a: [{ t: t0, v: 1 }, { t: t0 + 1000, v: 2 }], b: [{ t: t0 + 1000, v: 3 }] }, 1000, label)

    expect(rows).toEqual([{ label: '0', t: t0, a: 1, b: undefined }, { label: '1', t: t0 + 1000, a: 2, b: 3 }])
  })

  it('breaks the line where the PC was off', () => {
    const rows = toChartRows({ a: [{ t: t0, v: 1 }, { t: t0 + 31_000, v: 2 }] }, 1000, label)

    expect(rows.map(r => [r.label, r.a])).toEqual([['0', 1], ['15.5', undefined], ['31', 2]])
  })

  it('keeps labels unique when two points format the same', () => {
    const rows = toChartRows({ a: [{ t: t0, v: 1 }, { t: t0 + 1000, v: 2 }] }, 1000, () => '17:00')

    expect(new Set(rows.map(r => r.label)).size).toBe(2)
  })

  it('treats null readings as missing', () => {
    expect(toChartRows({ a: [{ t: t0, v: null }] }, 1000, label)[0]!.a).toBeUndefined()
  })
  it('can break on a single missing step, as history buckets need', () => {
    const rows = toChartRows({ a: [{ t: t0, v: 1 }, { t: t0 + 2000, v: 2 }] }, 1000, label, 1.5)

    expect(rows.map(r => r.a)).toEqual([1, undefined, 2])
  })
})
