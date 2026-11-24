<script setup lang="ts">
import type { IncidentDetail } from '~/api/types'

const props = defineProps<{ detail: IncidentDetail }>()

const { t, locale } = useI18n()

interface Entry { at: string, title: string, description?: string, icon: string }

const items = computed(() => {
  const detail = props.detail
  const entries: Entry[] = []
  if (detail.lastAliveAt) {
    entries.push({ at: detail.lastAliveAt, title: t('incident.timeline.lastAlive'), icon: 'i-lucide-heart-pulse' })
  }
  if (detail.telemetryEndAt) {
    entries.push({ at: detail.telemetryEndAt, title: t('incident.timeline.lastFrame'), icon: 'i-lucide-activity' })
  }
  const estimate = detail.summary.estimatedFailureAt
  if (estimate && estimate !== detail.telemetryEndAt && estimate !== detail.lastAliveAt) {
    entries.push({ at: estimate, title: t('incident.timeline.failure'), icon: 'i-lucide-zap-off' })
  }
  entries.push({ at: detail.summary.loggedAt, title: t('incident.timeline.logged'), description: 'Kernel-Power 41', icon: 'i-lucide-triangle-alert' })
  // The incident is the Kernel-Power 41 event itself: it already has its own entry.
  for (const event of detail.events.filter(e => !(e.provider === 'Microsoft-Windows-Kernel-Power' && e.eventId === 41))) {
    entries.push({
      at: event.loggedAt,
      title: t(eventKey(event.provider, event.eventId), { provider: event.provider, id: event.eventId }),
      description: `${event.provider} ${event.eventId}`,
      icon: 'i-lucide-scroll-text',
    })
  }
  return entries
    .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime())
    .map(entry => ({ date: formatTime(entry.at, locale.value), title: entry.title, description: entry.description, icon: entry.icon }))
})
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('incident.timeline.title') }}
      </h3>
    </template>
    <UTimeline
      :items="items"
      size="sm"
    />
  </UCard>
</template>
