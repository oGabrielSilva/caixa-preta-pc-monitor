<script setup lang="ts">
const { t } = useI18n()
const { incidents, problem } = useIncidents()
const stats = computed(() => summarize(incidents.value, new Date()))
</script>

<template>
  <UDashboardPanel :ui="{ body: '*:shrink-0' }">
    <template #header>
      <UDashboardNavbar :title="t('nav.incidents')" />
    </template>

    <template #body>
      <SharedStorageNotice
        v-if="problem"
        :problem="problem"
      />
      <UEmpty
        v-else-if="incidents.length === 0"
        icon="i-material-symbols-verified-user-outline-rounded"
        :title="t('incidents.empty.title')"
        :description="t('incidents.empty.description')"
      />
      <template v-else>
        <IncidentsIncidentStats :stats="stats" />
        <IncidentsPerDayChart :per-day="stats.perDay" />
        <IncidentsIncidentTable :incidents="incidents" />
      </template>
    </template>
  </UDashboardPanel>
</template>
