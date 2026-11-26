import { describe, expect, it } from 'vitest'
import type { SensorResponse } from '~/api/types'
import { createLiveBuffer } from '~/utils/live-buffer'
import { diskReadings } from '~/utils/storage-readings'
import { liveSnapshot } from '~~/test/support/fixtures'

function disk(index: number, measure: NonNullable<SensorResponse['storage']>['measure'], key: string, unit = '°C'): SensorResponse {
  return {
    key: `/nvme/${index}/${key}`, name: key, kind: unit === '%' ? 'Level' : 'Temperature', unit, role: 'None',
    range: measure === 'Life' ? { min: 10, max: 100 } : null, limit: null, critical: null, storage: { disk: index, measure },
  }
}

const sensors = [
  disk(0, 'Temperature', 'temperature/0'), disk(0, 'WarningTemperature', 'temperature/10'), disk(0, 'CriticalTemperature', 'temperature/11'),
  disk(0, 'UsedSpace', 'load/30', '%'), disk(0, 'Life', 'level/20', '%'),
]

const readingsOf = (values: (number | null)[]) =>
  diskReadings(createLiveBuffer({ ...liveSnapshot([]), sensors, frames: [{ t: 0, values }] }))

describe('diskReadings', () => {
  it('groups the measures of each disk', () => {
    expect(readingsOf([48, 89, 94, 80.5, 92])).toEqual([
      { disk: 0, temperature: 48, temperatureStatus: 'success', usedSpace: 80.5, life: 92, lifeStatus: 'success' },
    ])
  })

  it('colours the temperature by the limits the disk itself reports', () => {
    expect(readingsOf([90, 89, 94, 80, 92])[0]!.temperatureStatus).toBe('warning')
    expect(readingsOf([95, 89, 94, 80, 92])[0]!.temperatureStatus).toBe('error')
  })

  it('marks little life left out of range', () => {
    expect(readingsOf([48, 89, 94, 80, 8])[0]!.lifeStatus).toBe('error')
  })

  it('is neutral without the disk limits or a reading', () => {
    expect(readingsOf([48, null, null, null, null])[0]).toMatchObject({ temperatureStatus: 'neutral', life: null, lifeStatus: 'neutral' })
  })

  it('has no disks without storage sensors', () => {
    expect(diskReadings(null)).toEqual([])
  })
})
