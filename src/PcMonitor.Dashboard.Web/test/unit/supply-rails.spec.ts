import { describe, expect, it } from 'vitest'
import type { SensorResponse } from '~/api/types'
import { supplyRails } from '~/utils/supply-rails'

function sensor(key: string, kind: SensorResponse['kind'], role: SensorResponse['role'], range: SensorResponse['range'] = null): SensorResponse {
  return { key, name: key, kind, role, unit: kind === 'Voltage' ? 'V' : '%', range, limit: null, critical: null, storage: null }
}

describe('supplyRails', () => {
  const rail3V3 = sensor('/lpc/nct6799d/0/voltage/3', 'Voltage', 'Rail3V3', { min: 3.135, max: 3.465 })
  const rail12V = sensor('/lpc/nct6799d/0/voltage/4', 'Voltage', 'Rail12V', { min: 11.4, max: 12.6 })
  const rail5V = sensor('/lpc/nct6799d/0/voltage/1', 'Voltage', 'Rail5V', { min: 4.75, max: 5.25 })
  // The NVMe life left also has a safe range (10–100 %), but it is not a power supply voltage.
  const diskLife = { ...sensor('/nvme/0/level/20', 'Level', 'None', { min: 10, max: 100 }), storage: { disk: 0, measure: 'Life' as const } }
  const vcore = sensor('/lpc/nct6799d/0/voltage/0', 'Voltage', 'Vcore')

  it('lists the +12 V, +5 V and +3.3 V rails in that order', () => {
    expect(supplyRails([rail3V3, vcore, rail12V, rail5V]).map(s => s.role)).toEqual(['Rail12V', 'Rail5V', 'Rail3V3'])
  })

  it('leaves out other sensors that have a safe range, such as the disk life left', () => {
    expect(supplyRails([diskLife, rail12V]).map(s => s.key)).toEqual(['/lpc/nct6799d/0/voltage/4'])
  })

  it('leaves out a rail the API sent without a range', () => {
    expect(supplyRails([sensor('/lpc/x/voltage/4', 'Voltage', 'Rail12V')])).toEqual([])
  })
})
