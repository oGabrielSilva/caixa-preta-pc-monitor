<script setup lang="ts">
const props = defineProps<{ problem: string }>()

const { t } = useI18n()

const message = computed(() => {
  if (props.problem.startsWith('storage.')) {
    return t(`storage.${props.problem.slice('storage.'.length)}`)
  }
  return props.problem === 'network' ? t('storage.network') : t('storage.other', { problem: props.problem })
})
</script>

<template>
  <UAlert
    color="warning"
    variant="subtle"
    icon="i-material-symbols-database-off-outline-rounded"
    :title="t('storage.title')"
    :description="message"
  />
</template>
