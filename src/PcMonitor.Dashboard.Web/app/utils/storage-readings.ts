import type { SensorResponse } from '~/api/types'
import { latestValue, type LiveBuffer } from '~/utils/live-buffer'
import { statusOf, type SensorStatus } from '~/utils/sensor-status'

type Measure = NonNullable<SensorResponse['storage']>['measure']

/** What the live page shows for one disk. */
export interface DiskReading {
  readonly disk: number
  readonly temperature: number | null
  readonly temperatureStatus: SensorStatus
  readonly usedSpace: number | null
  readonly life: number | null
  readonly lifeStatus: SensorStatus
}

/**
 * One entry per disk, from the sensors the API marks as storage. The temperature is judged against the warning and
 * critical temperatures the disk itself reports; life left by the range the API sends.
 */
export function diskReadings(buffer: LiveBuffer | null): DiskReading[] {
  if (!buffer) {
    return []
  }
  const disks = [...new Set(buffer.sensors.flatMap(s => (s.storage ? [s.storage.disk] : [])))].sort((a, b) => a - b)
  return disks.map((disk) => {
    const sensor = (measure: Measure) => buffer.sensors.find(s => s.storage?.disk === disk && s.storage.measure === measure)
    const value = (measure: Measure) => {
      const found = sensor(measure)
      return found ? latestValue(buffer, found.key) : null
    }
    const temperature = value('Temperature')
    const warning = value('WarningTemperature')
    const critical = value('CriticalTemperature')
    const lifeSensor = sensor('Life')
    const life = value('Life')
    return {
      disk,
      temperature,
      temperatureStatus: temperatureStatus(temperature, warning, critical),
      usedSpace: value('UsedSpace'),
      life,
      lifeStatus: lifeSensor ? statusOf(life, lifeSensor) : 'neutral',
    }
  })
}

function temperatureStatus(value: number | null, warning: number | null, critical: number | null): SensorStatus {
  if (value === null || warning === null || critical === null) {
    return 'neutral'
  }
  return value >= critical ? 'error' : value >= warning ? 'warning' : 'success'
}
