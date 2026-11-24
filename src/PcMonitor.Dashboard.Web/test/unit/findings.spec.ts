import { describe, expect, it } from 'vitest'
import type { Finding } from '~/api/types'
import { findingParams, groupFindings } from '~/utils/findings'

const t = (key: string, named?: Record<string, unknown>) => named ? `${key}(${JSON.stringify(named)})` : key

function finding(overrides: Partial<Finding>): Finding {
  return { code: 'rail.out_of_tolerance', kind: 'Evidence', hypothesis: 'PowerSupply', supports: true, strength: 'Moderate', parameters: {}, ...overrides }
}

describe('groupFindings', () => {
  it('groups the same finding for two hypotheses into one item', () => {
    const params = { rail: '12V', observed: '11.28' }
    const grouped = groupFindings([
      finding({ parameters: params }),
      finding({ hypothesis: 'BoardPowerDelivery', parameters: { observed: '11.28', rail: '12V' } }),
      finding({ code: 'load.idle', hypothesis: 'BoardPowerDelivery', strength: 'Weak' }),
    ])

    expect(grouped.map(g => [g.code, g.hypotheses])).toEqual([
      ['rail.out_of_tolerance', ['PowerSupply', 'BoardPowerDelivery']],
      ['load.idle', ['BoardPowerDelivery']],
    ])
  })

  it('keeps for and against apart', () => {
    expect(groupFindings([finding({}), finding({ supports: false })])).toHaveLength(2)
  })
})

describe('findingParams', () => {
  it('formats invariant numbers in the user locale', () => {
    expect(findingParams({ observed: '11.28', min: '11.4', samples: '2' }, 'pt-BR', t))
      .toEqual({ observed: '11,28', min: '11,4', samples: '2' })
  })

  it('names rails, roles and sleep states', () => {
    expect(findingParams({ rail: '3.3V', rails: '12V,5V', role: 'CpuLoad', state: '4' }, 'pt-BR', t))
      .toEqual({ rail: '+3,3 V', rails: '+12 V, +5 V', role: 'roles.CpuLoad', state: 'sleepStates.4' })
  })

  it('lists the sources of a hardware error', () => {
    expect(findingParams({ sources: 'bugcheck:0x00000124,event:18,boot_errors:2' }, 'en', t).sources)
      .toBe('whea.bugcheck({"code":"0x00000124"}), whea.event({"id":"18"}), whea.bootErrors({"count":"2"})')
  })
})
