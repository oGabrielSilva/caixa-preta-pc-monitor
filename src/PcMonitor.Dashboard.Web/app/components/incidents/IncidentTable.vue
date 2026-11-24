<script setup lang="ts">
import { h, resolveComponent } from 'vue'
import type { TableColumn, TableRow } from '@nuxt/ui'
import type { IncidentSummary } from '~/api/types'

const props = defineProps<{ incidents: readonly IncidentSummary[] }>()

const { t, locale } = useI18n()
const UBadge = resolveComponent('UBadge')
const UIcon = resolveComponent('UIcon')
const ConfidenceBadge = resolveComponent('SharedConfidenceBadge')

function signal(color: string, icon: string, labelKey: string) {
  return h(UBadge, { color, variant: 'outline', size: 'sm', icon }, () => t(labelKey))
}

const columns = computed<TableColumn<IncidentSummary>[]>(() => [
  {
    id: 'when',
    header: t('incidents.table.when'),
    cell: ({ row }) => h('span', { class: 'tabular-nums font-medium' }, formatDateTime(failureTime(row.original).getTime(), locale.value)),
  },
  {
    id: 'precision',
    header: t('incidents.table.precision'),
    cell: ({ row }) => h('span', { class: 'text-sm text-muted' }, t(`incidents.precision.${row.original.timeSource}`)),
  },
  {
    id: 'cause',
    header: t('incidents.table.cause'),
    cell: ({ row }) => h('div', { class: 'flex items-center gap-2' }, [
      h('span', row.original.topHypothesis ? t(`hypotheses.${row.original.topHypothesis}`) : t('incidents.noConclusion')),
      h(ConfidenceBadge, { confidence: row.original.confidence }),
    ]),
  },
  {
    id: 'signals',
    header: t('incidents.table.signals'),
    cell: ({ row }) => h('div', { class: 'flex flex-wrap gap-1.5' }, [
      row.original.hasTelemetry ? signal('success', 'i-lucide-activity', 'incidents.badges.telemetry') : null,
      row.original.sleepState !== 0 ? signal('secondary', 'i-lucide-moon', 'incidents.badges.sleep') : null,
      row.original.observations.includes('boot.early_failure') ? signal('neutral', 'i-lucide-power', 'incidents.badges.earlyBoot') : null,
    ]),
  },
  { id: 'open', header: '', cell: () => h(UIcon, { name: 'i-lucide-chevron-right', class: 'size-4 text-dimmed' }) },
])

function open(_: Event, row: TableRow<IncidentSummary>) {
  void navigateTo(`/incidents/${row.original.id}`)
}
</script>

<template>
  <UCard :ui="{ body: 'p-0 sm:p-0' }">
    <UTable
      :data="[...props.incidents]"
      :columns="columns"
      class="cursor-pointer"
      @select="open"
    />
  </UCard>
</template>
