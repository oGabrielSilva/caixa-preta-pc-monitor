<script setup lang="ts">
import type { SeriesPoint } from '~/utils/live-buffer'

// The only place that builds chart data: rows with unique labels and breaks where nothing was recorded.
// `bars` draws one bar per point (counts per day) instead of a line.
const props = withDefaults(defineProps<{
  series: Readonly<Record<string, readonly SeriesPoint[]>>
  categories: readonly { key: string, label: string, color?: string }[]
  stepMs: number
  height?: number
  compact?: boolean
  format?: (t: number, locale: string) => string
  gapSteps?: number
  bars?: boolean
}>(), { height: 220, compact: false, format: undefined, gapSteps: 3, bars: false })

const { locale } = useI18n()

const rows = computed(() =>
  toChartRows(props.series, props.stepMs, t => props.format ? props.format(t, locale.value) : formatTime(t, locale.value), props.gapSteps))
</script>

<template>
  <UChart
    :type="props.bars ? 'bar' : props.compact ? 'area' : 'line'"
    :data="rows"
    index="label"
    :categories="[...props.categories]"
    :height="props.height"
    :curve="props.bars ? undefined : 'monotone'"
    :x-axis="!props.compact"
    :y-axis="!props.compact"
    :grid="!props.compact"
    :legend="props.compact ? false : undefined"
    :tooltip="!props.compact"
    :transition="false"
  />
</template>
