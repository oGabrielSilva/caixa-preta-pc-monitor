<script setup lang="ts">
import type { IncidentSummary } from '~/api/types'

const props = defineProps<{ incidents: readonly IncidentSummary[] }>()

const { t, locale } = useI18n()

const recent = computed(() => props.incidents.slice(0, 4))
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
        <h3 class="font-medium">
          {{ t('live.recent.title') }}
        </h3>
        <UButton
          v-if="props.incidents.length > 0"
          to="/incidents"
          variant="ghost"
          trailing-icon="i-lucide-arrow-right"
          size="sm"
        >
          {{ t('live.recent.seeAll', { count: props.incidents.length }) }}
        </UButton>
      </div>
    </template>
    <p
      v-if="recent.length === 0"
      class="text-sm text-muted"
    >
      {{ t('live.recent.none') }}
    </p>
    <ul
      v-else
      class="divide-y divide-default"
    >
      <li
        v-for="incident in recent"
        :key="incident.id"
      >
        <NuxtLink
          :to="`/incidents/${incident.id}`"
          class="flex items-center justify-between gap-3 rounded-md py-2.5 hover:bg-elevated/50"
        >
          <span class="flex items-center gap-3">
            <UIcon
              name="i-lucide-triangle-alert"
              class="size-4 text-error"
            />
            <span class="tabular-nums">{{ formatDateTime(failureTime(incident).getTime(), locale) }}</span>
          </span>
          <span class="flex items-center gap-2">
            <span class="text-sm text-muted">{{ incident.topHypothesis ? t(`hypotheses.${incident.topHypothesis}`) : t('incidents.noConclusion') }}</span>
            <SharedConfidenceBadge :confidence="incident.confidence" />
          </span>
        </NuxtLink>
      </li>
    </ul>
  </UCard>
</template>
