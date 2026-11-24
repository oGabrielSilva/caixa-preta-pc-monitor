<script setup lang="ts">
import type { SensorRole } from '~/api/types'
import type { LiveBuffer, SeriesPoint } from '~/utils/live-buffer'

const props = defineProps<{ buffer: LiveBuffer }>()

const { t } = useI18n()

// Series colours are categories, not statuses: raw palette colours, never success/warning/error.
const lines: { role: SensorRole, key: string, labelKey: string, color: string }[] = [
  { role: 'CpuTemperature', key: 'temperature', labelKey: 'live.trend.temperature', color: 'var(--color-amber-400)' },
  { role: 'CpuLoad', key: 'load', labelKey: 'live.trend.load', color: 'var(--color-sky-400)' },
  { role: 'CpuPackagePower', key: 'power', labelKey: 'live.trend.power', color: 'var(--color-violet-400)' },
]

const present = computed(() => lines.filter(line => sensorByRole(props.buffer, line.role)))
const series = computed(() => Object.fromEntries(present.value.map(line =>
  [line.key, seriesOf(props.buffer, sensorByRole(props.buffer, line.role)!.key)])) as Record<string, SeriesPoint[]>)
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('live.trend.title') }}
      </h3>
    </template>
    <SharedTimeSeriesChart
      :series="series"
      :categories="present.map(line => ({ key: line.key, label: t(line.labelKey), color: line.color }))"
      :step-ms="1000"
      :height="240"
    />
  </UCard>
</template>
