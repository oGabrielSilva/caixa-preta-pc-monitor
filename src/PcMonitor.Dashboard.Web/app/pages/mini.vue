<script setup lang="ts">
definePageMeta({ layout: 'mini' })

const { buffer, state, problem } = useLive()
const unseen = useUnseenIncident()
const readings = computed(() => miniReadings(buffer.value))
const dot = computed(() => recordingDot(state.value, problem.value))
</script>

<template>
  <MiniPanel
    :readings="readings"
    :dot="dot"
    :unseen-incident-id="unseen?.id ?? null"
    @open="openInMainWindow"
  />
</template>
