<script setup lang="ts">
import type { Finding } from '~/api/types'

/** `null` while the incident has no diagnosis yet. */
const props = defineProps<{ findings: readonly Finding[] | null }>()

const { t, locale } = useI18n()

const items = computed(() => groupFindings(props.findings ?? []).map((finding) => {
  const hypotheses = finding.hypotheses.map(h => t(`hypotheses.${h}`)).join(t('incident.weighed.and'))
  return {
    key: `${finding.code}-${finding.supports}-${JSON.stringify(finding.parameters)}`,
    text: t(findingKey(finding.code), { code: finding.code, ...findingParams(finding.parameters, locale.value, t) }),
    evidence: finding.kind === 'Evidence',
    supports: finding.supports,
    detail: finding.kind === 'Evidence'
      ? `${t(finding.supports ? 'incident.weighed.for' : 'incident.weighed.against', { hypotheses })} · ${t(`strength.${finding.strength}`)}`
      : null,
  }
}))
</script>

<template>
  <p
    v-if="items.length === 0"
    class="text-sm text-muted"
  >
    {{ props.findings === null ? t('incidents.notDiagnosed') : t('incident.weighed.none') }}
  </p>
  <ul
    v-else
    class="space-y-3"
  >
    <li
      v-for="item in items"
      :key="item.key"
      class="flex gap-3"
    >
      <UIcon
        :name="!item.evidence ? 'i-material-symbols-info-outline-rounded' : item.supports ? 'i-material-symbols-add-circle-outline-rounded' : 'i-material-symbols-do-not-disturb-on-outline-rounded'"
        class="mt-0.5 size-5 shrink-0"
        :class="!item.evidence ? 'text-info' : item.supports ? 'text-primary' : 'text-error'"
      />
      <div>
        <p
          class="text-sm"
          :class="{ 'text-muted': !item.evidence }"
        >
          {{ item.text }}
        </p>
        <p
          v-if="item.detail"
          class="text-xs text-dimmed"
        >
          {{ item.detail }}
        </p>
      </div>
    </li>
  </ul>
</template>
