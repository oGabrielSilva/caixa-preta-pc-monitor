<script setup lang="ts">
import type { Confidence } from '~/api/types'

const props = withDefaults(defineProps<{ confidence: Confidence | null, size?: 'sm' | 'md' | 'lg' }>(), { size: 'sm' })

const { t } = useI18n()

// Confidence is not a reading status: primary for a conclusion, warning when weak, neutral when there is none.
const color = computed(() => {
  switch (props.confidence) {
    case 'High':
    case 'Medium': return 'primary'
    case 'Low': return 'warning'
    default: return 'neutral'
  }
})
</script>

<template>
  <UBadge
    :color="color"
    variant="subtle"
    :size="props.size"
  >
    {{ props.confidence ? t(`confidence.${props.confidence}`) : t('incidents.notDiagnosed') }}
  </UBadge>
</template>
