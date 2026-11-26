import { describe, expect, it } from 'vitest'
import type { SensorResponse } from '~/api/types'
import { createLiveBuffer } from '~/utils/live-buffer'
import { miniReadings, recordingDot } from '~/utils/mini-readings'
import { cpuTemperature, liveSnapshot, rail12 } from '~~/test/support/fixtures'

const load: SensorResponse = { key: '/amdcpu/0/load/0', name: 'CPU Total', kind: 'Load', unit: '%', role: 'CpuLoad', range: null, limit: null, critical: null }
const rail5: SensorResponse = { ...rail12, key: '/lpc/nct6799d/0/voltage/1', name: '+5V', role: 'Rail5V', range: { min: 4.75, max: 5.25 } }

function buffer(values: (number | null)[], sensors = [cpuTemperature, load, rail12, rail5]) {
  return createLiveBuffer({ ...liveSnapshot([]), sensors, frames: [{ t: 0, values }] })
}

describe('miniReadings', () => {
  it('picks cpu, load and the three rails in a fixed order', () => {
    expect(miniReadings(buffer([45, 10, 12.19, 5])).map(r => r.role)).toEqual(['CpuTemperature', 'CpuLoad', 'Rail12V', 'Rail5V', 'Rail3V3'])
  })

  it('marks a rail outside the ATX range red and the cpu at 95 °C amber', () => {
    const readings = miniReadings(buffer([96, 10, 11.28, 5]))

    expect(readings.find(r => r.role === 'Rail12V')!.status).toBe('error')
    expect(readings.find(r => r.role === 'CpuTemperature')!.status).toBe('warning')
    expect(readings.find(r => r.role === 'Rail5V')!.status).toBe('success')
  })

  it('shows no value for a sensor without a reading or missing on the board', () => {
    const readings = miniReadings(buffer([null, 10, 12.19, 5]))

    expect(readings.find(r => r.role === 'CpuTemperature')!.value).toBeNull()
    expect(readings.find(r => r.role === 'Rail3V3')).toMatchObject({ value: null, status: 'neutral' })
  })

  it('has no values before the first snapshot', () => {
    expect(miniReadings(null).every(r => r.value === null)).toBe(true)
  })
})

describe('recordingDot', () => {
  it('is green only while recording and the dashboard answers', () => {
    expect(recordingDot('Recording', null)).toBe('success')
    expect(recordingDot('Stale', null)).toBe('error')
    expect(recordingDot('Stopped', null)).toBe('error')
    expect(recordingDot('Recording', 'network')).toBe('error')
  })
})
