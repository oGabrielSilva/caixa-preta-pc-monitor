<script setup lang="ts">
const { t } = useI18n()
const { sensors, problem: sensorsProblem } = useHistorySensors()
const { sensor, range } = useHistorySelection(sensors)
const { history, problem } = useHistory(sensor, range)
const { incidents } = useIncidents()
const { busy, exportHistory } = useExport()
</script>

<template>
  <SharedPage :title="t('nav.history')">
    <template #right>
      <SharedExportMenu
        v-if="history"
        :formats="['csv', 'xlsx']"
        :busy="busy"
        @select="format => history && exportHistory(format, history, incidents)"
      />
    </template>
    <template #toolbar>
      <HistoryToolbar
        v-model:sensor="sensor"
        v-model:range="range"
        :sensors="sensors"
      />
    </template>

    <SharedStorageNotice
      v-if="sensorsProblem ?? problem"
      :problem="(sensorsProblem ?? problem)!"
    />
    <UEmpty
      v-else-if="sensors.length === 0"
      icon="i-material-symbols-show-chart-rounded"
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
  </SharedPage>
</template>
