<script setup lang="ts">
import type { HistoryRange, HistoryResponse, IncidentSummary } from '~/api/types'

const props = defineProps<{ history: HistoryResponse, range: HistoryRange, incidents: readonly IncidentSummary[] }>()

const { t, locale } = useI18n()

const series = computed(() => ({
  max: props.history.buckets.map(b => ({ t: b.t, v: b.max })),
  avg: props.history.buckets.map(b => ({ t: b.t, v: b.avg })),
  min: props.history.buckets.map(b => ({ t: b.t, v: b.min })),
}))

const sensorName = computed(() => props.history.sensor.role === 'None' ? props.history.sensor.name : t(`roles.${props.history.sensor.role}`))

const drops = computed(() => {
  const from = new Date(props.history.from).getTime()
  const to = new Date(props.history.to).getTime()
  return props.incidents.filter((incident) => {
    const time = failureTime(incident).getTime()
    return time >= from && time <= to
  })
})
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex flex-wrap items-center justify-between gap-2">
        <h3 class="font-medium">
          {{ t('history.chartTitle', { sensor: sensorName, range: t(`history.ranges.${props.range}`) }) }}
        </h3>
        <span class="text-xs text-dimmed">{{ t('history.buckets', { minutes: props.history.bucketSeconds / 60 }) }}</span>
      </div>
    </template>
    <p
      v-if="props.history.buckets.length === 0"
      class="text-sm text-muted"
    >
      {{ t('history.noData') }}
    </p>
    <SharedTimeSeriesChart
      v-else
      :series="series"
      :categories="[
        { key: 'max', label: t('history.series.max'), color: 'var(--color-neutral-500)' },
        { key: 'avg', label: t('history.series.avg'), color: 'var(--ui-primary)' },
        { key: 'min', label: t('history.series.min'), color: 'var(--color-neutral-500)' },
      ]"
      :step-ms="props.history.bucketSeconds * 1000"
      :height="320"
      :format="formatDateTime"
      :gap-steps="1.5"
    />
    <div
      v-if="drops.length > 0"
      class="mt-4 flex flex-wrap items-center gap-2 text-sm"
    >
      <span class="text-muted">{{ t('history.drops') }}</span>
      <UButton
        v-for="drop in drops"
        :key="drop.id"
        :to="`/incidents/${drop.id}`"
        color="error"
        variant="subtle"
        size="xs"
        icon="i-lucide-triangle-alert"
      >
        {{ formatDateTime(failureTime(drop).getTime(), locale) }}
      </UButton>
    </div>
  </UCard>
</template>
