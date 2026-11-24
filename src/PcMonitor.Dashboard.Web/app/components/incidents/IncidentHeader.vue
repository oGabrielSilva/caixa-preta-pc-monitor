<script setup lang="ts">
import type { IncidentDetail } from '~/api/types'

const props = defineProps<{ detail: IncidentDetail }>()

const { t } = useI18n()

const summary = computed(() => props.detail.summary)
const summaryKey = computed(() => props.detail.diagnosis && summary.value.confidence
  ? `incident.summary.${summary.value.confidence}`
  : 'incident.summary.none')
</script>

<template>
  <UCard>
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-sm text-muted">
          {{ t('incident.mostLikely') }}
        </p>
        <p class="mt-1 text-2xl font-semibold">
          {{ summary.topHypothesis ? t(`hypotheses.${summary.topHypothesis}`) : t('incidents.noConclusion') }}
        </p>
        <p class="mt-1 max-w-prose text-sm text-muted">
          {{ t(summaryKey) }}
        </p>
      </div>
      <SharedConfidenceBadge
        :confidence="summary.confidence"
        size="lg"
      />
    </div>
    <div
      v-if="props.detail.diagnosis"
      class="mt-6"
    >
      <p class="mb-3 text-xs font-medium uppercase tracking-wide text-dimmed">
        {{ t('incident.ranking') }}
      </p>
      <IncidentsHypothesisRanking :ranking="props.detail.diagnosis.ranking" />
    </div>
  </UCard>
</template>
