<script setup lang="ts">
import type { HistoryBucket, SensorResponse } from '~/api/types'

const props = defineProps<{ sensor: SensorResponse, buckets: readonly HistoryBucket[] }>()

const { t, locale } = useI18n()

const stats = computed(() => {
  const empty = props.buckets.length === 0
  const values = {
    min: empty ? null : Math.min(...props.buckets.map(b => b.min)),
    avg: empty ? null : props.buckets.reduce((sum, b) => sum + b.avg, 0) / props.buckets.length,
    max: empty ? null : Math.max(...props.buckets.map(b => b.max)),
  }
  return (['min', 'avg', 'max'] as const).map(key => ({
    key,
    label: t(`history.${key}`),
    value: `${formatNumber(values[key], locale.value, digitsFor(props.sensor.kind))} ${props.sensor.unit}`,
  }))
})
</script>

<template>
  <div class="grid gap-4 sm:grid-cols-3">
    <SharedStatCard
      v-for="stat in stats"
      :key="stat.key"
      :label="stat.label"
      icon="i-lucide-sigma"
      :value="stat.value"
    />
  </div>
</template>
