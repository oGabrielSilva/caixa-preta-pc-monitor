// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import HistoryStats from '~/components/history/HistoryStats.vue'
import { mountPtBR } from '~~/test/support/nuxt'
import { rail12 } from '~~/test/support/fixtures'

describe('HistoryStats', () => {
  it('shows min, mean and max of the buckets in the user locale', async () => {
    const stats = await mountPtBR(HistoryStats, { props: { sensor: rail12, buckets: [
      { t: 0, min: 12.17, avg: 12.18, max: 12.2 },
      { t: 900_000, min: 12.18, avg: 12.2, max: 12.21 },
    ] } })

    const text = stats.text()
    expect(text).toContain('12,17')
    expect(text).toContain('12,19')
    expect(text).toContain('12,21')
  })

  it('shows dashes without buckets', async () => {
    const stats = await mountPtBR(HistoryStats, { props: { sensor: rail12, buckets: [] } })

    expect(stats.text().match(/—/g)).toHaveLength(3)
  })
})
