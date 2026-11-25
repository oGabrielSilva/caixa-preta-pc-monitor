<script setup lang="ts">
import type { SensorRole } from '~/api/types'
import type { LiveBuffer } from '~/utils/live-buffer'

const props = defineProps<{ buffer: LiveBuffer }>()

const { t, locale } = useI18n()

const cards: { role: SensorRole, labelKey: string, icon: string, digits: number }[] = [
  { role: 'CpuTemperature', labelKey: 'live.cards.cpuTemperature', icon: 'i-material-symbols-device-thermostat-rounded', digits: 1 },
  { role: 'Rail12V', labelKey: 'live.cards.rail12', icon: 'i-material-symbols-bolt-outline-rounded', digits: 2 },
  { role: 'CpuPackagePower', labelKey: 'live.cards.power', icon: 'i-material-symbols-electric-meter-outline-rounded', digits: 1 },
  { role: 'CpuLoad', labelKey: 'live.cards.load', icon: 'i-material-symbols-speed-outline-rounded', digits: 0 },
]

const metrics = computed(() => cards.flatMap((card) => {
  const sensor = sensorByRole(props.buffer, card.role)
  if (!sensor) {
    return []
  }
  const value = latestValue(props.buffer, sensor.key)
  const hint = sensor.range
    ? t('live.hints.range', { min: formatNumber(sensor.range.min, locale.value, 2), max: formatNumber(sensor.range.max, locale.value, 2) })
    : sensor.limit !== null ? t('live.hints.limit', { limit: sensor.limit }) : undefined
  return [{ ...card, sensor, value, hint, status: statusOf(value, sensor), series: seriesOf(props.buffer, sensor.key) }]
}))
</script>

<template>
  <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
    <SharedMetricCard
      v-for="metric in metrics"
      :key="metric.role"
      :label="t(metric.labelKey)"
      :icon="metric.icon"
      :value="metric.value"
      :unit="metric.sensor.unit"
      :digits="metric.digits"
      :status="metric.status"
      :hint="metric.hint"
      :series="metric.series"
    />
  </div>
</template>
