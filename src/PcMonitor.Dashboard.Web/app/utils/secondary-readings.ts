import type { SensorResponse } from '~/api/types'

/** How a reading is named: an i18n key (with the memory slot) or the name the sensor was recorded with. */
export type ReadingLabel = { readonly key: string, readonly slot?: number } | { readonly text: string }

export interface SecondaryReading {
  readonly sensor: SensorResponse
  readonly label: ReadingLabel
  readonly value: number | null
}

const dimm = /^\/memory\/dimm\/(\d+)\//

/**
 * The readings shown under the supply voltages: Vcore, the fans that are spinning and the memory temperatures.
 * A fan header reading 0 RPM has nothing plugged in; the board's unnamed temperatures mean nothing without a profile
 * that names them. Both stay available in the history.
 */
export function secondaryReadings(sensors: readonly SensorResponse[], valueOf: (key: string) => number | null): SecondaryReading[] {
  return sensors.flatMap((sensor): SecondaryReading[] => {
    const value = valueOf(sensor.key)
    if (sensor.role === 'Vcore') {
      return [{ sensor, label: { key: 'live.others.vcore' }, value }]
    }
    if (sensor.role !== 'None') {
      return []
    }
    if (sensor.kind === 'Fan') {
      return value === 0 ? [] : [{ sensor, label: { text: sensor.name }, value }]
    }
    const slot = dimm.exec(sensor.key)
    return slot && sensor.kind === 'Temperature' ? [{ sensor, label: { key: 'live.others.memory', slot: Number(slot[1]) + 1 }, value }] : []
  })
}
