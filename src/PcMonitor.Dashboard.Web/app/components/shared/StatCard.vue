<script setup lang="ts">
const props = defineProps<{
  label: string
  icon: string
  value: string | number
  hint?: string
}>()
</script>

<template>
  <UCard :ui="{ body: 'p-4 sm:p-4' }">
    <div class="flex items-center gap-2 text-sm text-muted">
      <UIcon
        :name="props.icon"
        class="size-4"
      />{{ props.label }}
    </div>
    <p class="mt-2 text-3xl font-semibold tabular-nums">
      {{ props.value }}
    </p>
    <p
      v-if="props.hint"
      class="text-xs text-dimmed"
    >
      {{ props.hint }}
    </p>
  </UCard>
</template>
