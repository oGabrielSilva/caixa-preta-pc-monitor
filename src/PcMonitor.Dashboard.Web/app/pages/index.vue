<script setup lang="ts">
const { t } = useI18n()
const { buffer, state, problem, session, lastFrameAt } = useLive()
const { incidents } = useIncidents()
const disks = computed(() => diskReadings(buffer.value))
</script>

<template>
  <SharedPage :title="t('nav.live')">
    <template #right>
      <LiveStatus
        :state="state"
        :session="session"
        :last-frame-at="lastFrameAt"
      />
    </template>

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
      <LiveStorageCard :disks="disks" />
      <LiveRecentIncidents :incidents="incidents" />
    </template>
  </SharedPage>
</template>
