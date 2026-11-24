// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import { mountPtBR } from '~~/test/support/nuxt'
import MetricCard from '~/components/shared/MetricCard.vue'

describe('MetricCard', () => {
  it('shows a dash and a neutral status without a reading', async () => {
    const card = await mountPtBR(MetricCard, { props: { label: 'CPU', icon: 'i-lucide-thermometer', value: null, unit: '°C', status: 'neutral' } })

    expect(card.text()).toContain('—')
    expect(card.text()).not.toContain('Normal')
  })

  it('shows the value in the locale with its status', async () => {
    const card = await mountPtBR(MetricCard, { props: { label: '+12 V', icon: 'i-lucide-zap', value: 11.28, digits: 2, unit: 'V', status: 'error' } })

    expect(card.text()).toContain('11,28')
    expect(card.text()).toContain('Fora da faixa')
  })
})
