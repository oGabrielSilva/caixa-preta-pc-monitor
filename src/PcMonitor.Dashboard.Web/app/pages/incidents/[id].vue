<script setup lang="ts">
const { t, locale } = useI18n()
const route = useRoute()
const { incident, problem } = useIncident(() => Number(route.params.id))
const title = computed(() => incident.value
  ? t('incident.title', { date: formatFullDateTime(incident.value.summary.estimatedFailureAt ?? incident.value.summary.loggedAt, locale.value) })
  : t('nav.incidents'))
</script>

<template>
  <UDashboardPanel :ui="{ body: '*:shrink-0' }">
    <template #header>
      <UDashboardNavbar :title="title">
        <template #leading>
          <UButton
            to="/incidents"
            icon="i-material-symbols-arrow-back-rounded"
            variant="ghost"
            color="neutral"
            :aria-label="t('incident.back')"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <UEmpty
        v-if="problem === 'http.404'"
        icon="i-material-symbols-search-off-rounded"
        :title="t('incident.notFound')"
      />
      <SharedStorageNotice
        v-else-if="problem"
        :problem="problem"
      />
      <template v-else-if="incident">
        <div class="grid gap-4 lg:grid-cols-3">
          <IncidentsIncidentHeader
            :detail="incident"
            class="lg:col-span-2"
          />
          <IncidentsIncidentTimeline :detail="incident" />
        </div>
        <IncidentsFailureTelemetry :telemetry="incident.telemetry" />
        <div class="grid gap-4 lg:grid-cols-2">
          <UCard>
            <template #header>
              <h3 class="font-medium">
                {{ t('incident.weighed.title') }}
              </h3>
            </template>
            <IncidentsFindingList :findings="incident.diagnosis?.findings ?? null" />
          </UCard>
          <UCard>
            <template #header>
              <h3 class="font-medium">
                {{ t('incident.limits.title') }}
              </h3>
            </template>
            <IncidentsLimitationList :limitations="incident.diagnosis?.limitations ?? null" />
          </UCard>
        </div>
      </template>
    </template>
  </UDashboardPanel>
</template>
