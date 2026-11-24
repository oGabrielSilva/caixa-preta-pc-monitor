<script setup lang="ts">
import type { SeriesPoint } from '~/utils/live-buffer'
import type { SensorStatus } from '~/utils/sensor-status'

const props = withDefaults(defineProps<{
  label: string
  icon: string
  value: number | null
  unit: string
  status: SensorStatus
  digits?: number
  hint?: string
  series?: readonly SeriesPoint[]
}>(), { digits: 1, hint: undefined, series: undefined })

const { t, locale } = useI18n()
</script>

<template>
  <UCard
    :ui="{ body: 'p-4 sm:p-4' }"
    class="overflow-hidden"
  >
    <div class="flex items-start justify-between gap-2">
      <div class="flex items-center gap-2 text-sm text-muted">
        <UIcon
          :name="props.icon"
          class="size-4"
        />
        {{ props.label }}
      </div>
      <UBadge
        v-if="props.status !== 'neutral'"
        :color="props.status"
        variant="subtle"
        size="sm"
      >
        {{ t(`status.${props.status}`) }}
      </UBadge>
    </div>
    <div class="mt-2 flex items-baseline gap-1">
      <span class="text-3xl font-semibold tabular-nums tracking-tight">{{ formatNumber(props.value, locale, props.digits) }}</span>
      <span class="text-sm text-muted">{{ props.unit }}</span>
    </div>
    <p
      v-if="props.hint"
      class="mt-1 text-xs text-dimmed"
    >
      {{ props.hint }}
    </p>
    <SharedTimeSeriesChart
      v-if="props.series && props.series.length > 1"
      :series="{ value: props.series }"
      :categories="[{ key: 'value', label: props.label }]"
      :step-ms="1000"
      :height="56"
      compact
      class="-mx-4 -mb-4 mt-3"
    />
  </UCard>
</template>
