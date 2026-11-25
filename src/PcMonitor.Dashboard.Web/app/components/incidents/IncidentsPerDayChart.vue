<script setup lang="ts">
import type { IncidentStats } from '~/utils/incident-stats'

const props = defineProps<{ perDay: IncidentStats['perDay'] }>()

const { t } = useI18n()

const dayStart = (day: string) => {
  const [year, month, date] = day.split('-').map(Number)
  return new Date(year!, month! - 1, date!).getTime()
}

const series = computed(() => ({ count: props.perDay.map(d => ({ t: dayStart(d.day), v: d.count })) }))
// Over more than one calendar year, `24/11` would repeat: show the year then.
const withYear = computed(() => props.perDay.length > 0 && props.perDay[0]!.day.slice(0, 4) !== props.perDay.at(-1)!.day.slice(0, 4))
const format = (time: number, locale: string) => formatDay(localDay(new Date(time)), locale, withYear.value)
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('incidents.perDay.title') }}
      </h3>
    </template>
    <SharedTimeSeriesChart
      :series="series"
      :categories="[{ key: 'count', label: t('incidents.perDay.series'), color: 'error' }]"
      :step-ms="24 * 60 * 60 * 1000"
      :gap-steps="Number.POSITIVE_INFINITY"
      :height="180"
      :format="format"
      bars
    />
  </UCard>
</template>
