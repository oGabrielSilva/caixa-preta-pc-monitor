// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import StorageCard from '~/components/live/StorageCard.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('StorageCard', () => {
  it('shows each disk and a dash for a missing reading', async () => {
    const card = await mountPtBR(StorageCard, { props: { disks: [
      { disk: 0, temperature: 48, temperatureStatus: 'success', usedSpace: 80.2, life: null, lifeStatus: 'neutral' },
    ] } })

    expect(card.text()).toContain('Disco 1')
    expect(card.text()).toContain('48')
    expect(card.text()).toContain('80')
    expect(card.text()).toContain('—')
  })
})
