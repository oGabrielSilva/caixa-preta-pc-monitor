// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import HypothesisRanking from '~/components/incidents/HypothesisRanking.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('HypothesisRanking', () => {
  it('draws positive scores to the right and negative to the left', async () => {
    const ranking = await mountPtBR(HypothesisRanking, { props: { ranking: [
      { hypothesis: 'BoardPowerDelivery', score: 3 },
      { hypothesis: 'Thermal', score: -2 },
    ] } })

    const bars = ranking.findAll('[data-direction]')
    expect(bars.map(bar => bar.attributes('data-direction'))).toEqual(['for', 'against'])
    expect(bars[0]!.attributes('style')).toContain('width: 50%')
    expect(ranking.text()).toContain('Placa / alimentação')
    expect(ranking.text()).toContain('+3')
    expect(ranking.text()).toContain('-2')
  })
})
