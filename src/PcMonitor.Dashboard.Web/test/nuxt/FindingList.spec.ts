// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import FindingList from '~/components/incidents/FindingList.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('FindingList', () => {
  it('renders a grouped finding once with both hypotheses', async () => {
    const parameters = { rail: '12V', min: '11.4', max: '12.6', observed: '11.28', samples: '3' }
    const list = await mountPtBR(FindingList, { props: { findings: [
      { code: 'rail.out_of_tolerance', kind: 'Evidence', hypothesis: 'PowerSupply', supports: true, strength: 'Moderate', parameters },
      { code: 'rail.out_of_tolerance', kind: 'Evidence', hypothesis: 'BoardPowerDelivery', supports: true, strength: 'Moderate', parameters },
      { code: 'restart.quick', kind: 'Observation', hypothesis: null, supports: true, strength: 'Weak', parameters: { seconds: '91' } },
    ] } })

    expect(list.findAll('li')).toHaveLength(2)
    expect(list.text()).toContain('O +12 V chegou a 11,28 V (3 leituras)')
    expect(list.text()).toContain('a favor de Fonte e Placa / alimentação · moderada')
    expect(list.text()).toContain('Voltou a ligar 91 s depois')
  })
})
