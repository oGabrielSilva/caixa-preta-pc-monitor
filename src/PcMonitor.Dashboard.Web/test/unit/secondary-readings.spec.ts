import { describe, expect, it } from 'vitest'
import type { SensorResponse } from '~/api/types'
import { secondaryReadings } from '~/utils/secondary-readings'

function sensor(key: string, name: string, kind: SensorResponse['kind'], role: SensorResponse['role'] = 'None'): SensorResponse {
  return { key, name, kind, role, unit: '', range: null, limit: null, critical: null }
}

const sensors = [
  sensor('/lpc/nct6799d/0/voltage/0', 'Vcore', 'Voltage', 'Vcore'),
  sensor('/lpc/nct6799d/0/fan/0', 'Fan #1', 'Fan'),
  sensor('/lpc/nct6799d/0/fan/2', 'Fan #3', 'Fan'),
  sensor('/lpc/nct6799d/0/temperature/1', 'Temperature #2', 'Temperature'),
  sensor('/memory/dimm/1/temperature/0', 'Temperature', 'Temperature'),
  sensor('/amdcpu/0/temperature/2', 'CPU (Tctl/Tdie)', 'Temperature', 'CpuTemperature'),
]
const values: Record<string, number | null> = {
  '/lpc/nct6799d/0/voltage/0': 1.19, '/lpc/nct6799d/0/fan/0': 2033, '/lpc/nct6799d/0/fan/2': 0,
  '/lpc/nct6799d/0/temperature/1': 11, '/memory/dimm/1/temperature/0': 33, '/amdcpu/0/temperature/2': 46,
}

describe('secondaryReadings', () => {
  it('shows Vcore, spinning fans and memory, and leaves out unnamed board temperatures', () => {
    const readings = secondaryReadings(sensors, key => values[key] ?? null)

    expect(readings.map(r => [r.label, r.value])).toEqual([
      [{ key: 'live.others.vcore' }, 1.19],
      [{ text: 'Fan #1' }, 2033],
      [{ key: 'live.others.memory', slot: 2 }, 33],
    ])
  })

  it('keeps a fan without a reading (it may be read again) but not one that reads 0', () => {
    const readings = secondaryReadings([sensor('/lpc/nct6799d/0/fan/1', 'Fan #2', 'Fan')], () => null)

    expect(readings).toHaveLength(1)
  })
})
