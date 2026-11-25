<script setup lang="ts">
import type { HistoryRange, SensorResponse } from '~/api/types'

const props = defineProps<{ sensors: readonly SensorResponse[] }>()
const sensor = defineModel<string | undefined>('sensor', { required: true })
const range = defineModel<HistoryRange>('range', { required: true })

const { t } = useI18n()

const sensorItems = computed(() => props.sensors.map(s => ({
  label: s.role === 'None' ? s.name : `${t(`roles.${s.role}`)} (${s.name})`,
  value: s.key,
})))
const rangeItems = computed(() => historyRanges.map(value => ({ label: t(`history.ranges.${value}`), value })))
</script>

<template>
  <UDashboardToolbar>
    <template #left>
      <USelect
        v-model="sensor"
        :items="sensorItems"
        :placeholder="t('history.sensor')"
        :aria-label="t('history.sensor')"
        icon="i-material-symbols-monitoring-rounded"
        class="w-72"
      />
    </template>
    <template #right>
      <UTabs
        v-model="range"
        :items="rangeItems"
        :content="false"
        size="xs"
      />
    </template>
  </UDashboardToolbar>
</template>
