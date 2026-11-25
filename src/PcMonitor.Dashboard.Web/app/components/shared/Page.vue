<script setup lang="ts">
// One page inside the layout: a title bar (menu toggle, optional leading and right actions), an optional toolbar, the
// content.
const props = defineProps<{ title: string }>()

const { t } = useI18n()
const open = useSidebar()
</script>

<template>
  <div class="flex min-h-full flex-col">
    <div class="sticky top-0 z-5 flex h-12 shrink-0 items-center justify-between gap-3 border-b border-default bg-default/90 px-4 backdrop-blur">
      <div class="flex min-w-0 items-center gap-2">
        <UButton
          :icon="open ? 'i-material-symbols-left-panel-close-outline-rounded' : 'i-material-symbols-left-panel-open-outline-rounded'"
          color="neutral"
          variant="ghost"
          size="sm"
          :aria-label="t('layout.toggleSidebar')"
          @click="open = !open"
        />
        <slot name="leading" />
        <h1 class="truncate font-semibold">
          {{ props.title }}
        </h1>
      </div>
      <div class="flex shrink-0 items-center gap-2">
        <slot name="right" />
      </div>
    </div>
    <slot name="toolbar" />
    <div class="flex flex-1 flex-col gap-4 p-4 sm:p-6">
      <slot />
    </div>
  </div>
</template>
