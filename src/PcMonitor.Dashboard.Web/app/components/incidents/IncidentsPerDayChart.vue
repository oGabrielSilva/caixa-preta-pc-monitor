<script setup lang="ts">
import type { IncidentStats } from '~/utils/incident-stats'

const props = defineProps<{ perDay: IncidentStats['perDay'] }>()

const { t, locale } = useI18n()

const rows = computed(() => props.perDay.map(d => ({ day: formatDay(d.day, locale.value), count: d.count })))
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('incidents.perDay.title') }}
      </h3>
    </template>
    <UChart
      type="bar"
      :data="rows"
      index="day"
      :categories="[{ key: 'count', label: t('incidents.perDay.series'), color: 'error' }]"
      :height="180"
      :legend="false"
      :transition="false"
    />
  </UCard>
</template>
