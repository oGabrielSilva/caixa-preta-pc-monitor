<script setup lang="ts">
// Temporary content for pages whose features arrive in later plans.
const props = defineProps<{
  titleKey: string
  descriptionKey: string
  icon: string
}>()

const { t } = useI18n()
</script>

<template>
  <UDashboardPanel>
    <template #header>
      <UDashboardNavbar :title="t(props.titleKey)" />
    </template>

    <template #body>
      <UEmpty
        :icon="props.icon"
        :title="t('placeholder.title')"
        :description="t(props.descriptionKey)"
      />
    </template>
  </UDashboardPanel>
</template>
