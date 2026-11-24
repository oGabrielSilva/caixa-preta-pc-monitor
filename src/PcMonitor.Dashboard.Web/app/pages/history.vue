<script setup lang="ts">
const { t } = useI18n()
const { sensors, problem: sensorsProblem } = useHistorySensors()
const { sensor, range } = useHistorySelection(sensors)
const { history, problem } = useHistory(sensor, range)
const { incidents } = useIncidents()
</script>

<template>
  <UDashboardPanel :ui="{ body: '*:shrink-0' }">
    <template #header>
      <UDashboardNavbar :title="t('nav.history')" />
      <HistoryToolbar
        v-model:sensor="sensor"
        v-model:range="range"
        :sensors="sensors"
      />
    </template>

    <template #body>
      <SharedStorageNotice
        v-if="sensorsProblem ?? problem"
        :problem="(sensorsProblem ?? problem)!"
      />
      <UEmpty
        v-else-if="sensors.length === 0"
        icon="i-lucide-chart-line"
        :title="t('history.noSensors')"
      />
      <template v-else-if="history">
        <HistoryStats
          :sensor="history.sensor"
          :buckets="history.buckets"
        />
        <HistoryChart
          :history="history"
          :range="range"
          :incidents="incidents"
        />
      </template>
    </template>
  </UDashboardPanel>
</template>
