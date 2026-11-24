<script setup lang="ts">
import type { IncidentStats } from '~/utils/incident-stats'

const props = defineProps<{ stats: IncidentStats }>()

const { t, locale } = useI18n()

const cards = computed(() => [
  { label: t('incidents.stats.total'), icon: 'i-lucide-triangle-alert', value: props.stats.total,
    hint: props.stats.perDay[0] ? t('incidents.stats.totalHint', { date: formatDay(props.stats.perDay[0].day, locale.value) }) : undefined },
  { label: t('incidents.stats.last24h'), icon: 'i-lucide-clock', value: props.stats.last24h, hint: t('incidents.stats.last24hHint') },
  { label: t('incidents.stats.earlyBoot'), icon: 'i-lucide-power', value: props.stats.earlyBoot, hint: t('incidents.stats.earlyBootHint') },
  { label: t('incidents.stats.duringSleep'), icon: 'i-lucide-moon', value: props.stats.duringSleep, hint: t('incidents.stats.duringSleepHint') },
])
</script>

<template>
  <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
    <SharedStatCard
      v-for="card in cards"
      :key="card.label"
      v-bind="card"
    />
  </div>
</template>
