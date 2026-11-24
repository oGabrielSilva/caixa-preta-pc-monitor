<script setup lang="ts">
const { t } = useI18n()
const { buffer, state, problem, session, lastFrameAt } = useLive()
const { incidents } = useIncidents()
</script>

<template>
  <UDashboardPanel :ui="{ body: '*:shrink-0' }">
    <template #header>
      <UDashboardNavbar :title="t('nav.live')">
        <template #right>
          <LiveStatus
            :state="state"
            :session="session"
            :last-frame-at="lastFrameAt"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <SharedStorageNotice
        v-if="problem"
        :problem="problem"
      />
      <template v-else-if="buffer">
        <LiveSourceNotice :session="session" />
        <LiveMetrics :buffer="buffer" />
        <div class="grid gap-4 lg:grid-cols-3">
          <LiveTrendChart
            :buffer="buffer"
            class="lg:col-span-2"
          />
          <LiveRails :buffer="buffer" />
        </div>
        <LiveRecentIncidents :incidents="incidents" />
      </template>
    </template>
  </UDashboardPanel>
</template>
