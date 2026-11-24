// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import { mountPtBR } from '~~/test/support/nuxt'
import RailGauge from '~/components/live/RailGauge.vue'

describe('RailGauge', () => {
  it('places the marker inside the band for an in-range rail', async () => {
    const gauge = await mountPtBR(RailGauge, { props: { name: '+12 V', value: 12.19, range: { min: 11.4, max: 12.6 } } })

    const left = Number.parseFloat(gauge.get('[data-marker]').attributes('style')!.match(/left:\s*([\d.]+)%/)![1]!)
    expect(left).toBeGreaterThan(10)
    expect(left).toBeLessThan(90)
    expect(gauge.get('[data-marker]').classes()).toContain('bg-success')
    expect(gauge.text()).toContain('12,190 V')
  })

  it('marks an out-of-range rail in error at the edge', async () => {
    const gauge = await mountPtBR(RailGauge, { props: { name: '+12 V', value: 11, range: { min: 11.4, max: 12.6 } } })

    expect(gauge.get('[data-marker]').classes()).toContain('bg-error')
    expect(gauge.get('[data-marker]').attributes('style')).toContain('left: 0%')
  })
})
