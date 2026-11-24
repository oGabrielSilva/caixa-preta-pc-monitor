<script setup lang="ts">
import type { SeriesPoint } from '~/utils/live-buffer'

// The only place that builds chart data: rows with unique labels and breaks where nothing was recorded.
const props = withDefaults(defineProps<{
  series: Readonly<Record<string, readonly SeriesPoint[]>>
  categories: readonly { key: string, label: string, color?: string }[]
  stepMs: number
  height?: number
  compact?: boolean
  format?: (t: number, locale: string) => string
}>(), { height: 220, compact: false, format: undefined })

const { locale } = useI18n()

const rows = computed(() =>
  toChartRows(props.series, props.stepMs, t => props.format ? props.format(t, locale.value) : formatTime(t, locale.value)))
</script>

<template>
  <UChart
    :type="props.compact ? 'area' : 'line'"
    :data="rows"
    index="label"
    :categories="[...props.categories]"
    :height="props.height"
    curve="monotone"
    :x-axis="!props.compact"
    :y-axis="!props.compact"
    :grid="!props.compact"
    :legend="props.compact ? false : undefined"
    :tooltip="!props.compact"
    :transition="false"
  />
</template>
