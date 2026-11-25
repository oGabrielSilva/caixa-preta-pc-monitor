<script setup lang="ts">
import type { Finding } from '~/api/types'

/** `null` while the incident has no diagnosis yet. */
const props = defineProps<{ limitations: readonly Finding[] | null }>()

const { t, locale } = useI18n()

const items = computed(() => groupFindings(props.limitations ?? []).map(limitation => ({
  key: `${limitation.code}-${JSON.stringify(limitation.parameters)}`,
  text: t(findingKey(limitation.code), { code: limitation.code, ...findingParams(limitation.parameters, locale.value, t) }),
})))
</script>

<template>
  <p
    v-if="items.length === 0"
    class="text-sm text-muted"
  >
    {{ props.limitations === null ? t('incidents.notDiagnosed') : t('incident.limits.none') }}
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
        name="i-material-symbols-report-outline-rounded"
        class="mt-0.5 size-5 shrink-0 text-warning"
      />
      <p class="text-sm text-muted">
        {{ item.text }}
      </p>
    </li>
  </ul>
</template>
