<script setup lang="ts">
// Provisional shell: the final visual design is approved with mockups in Plan 05.
const { t } = useI18n()
const { version } = useAppVersion()

const links = computed(() =>
  navigationItems.map(item => ({ label: t(item.labelKey), icon: item.icon, to: item.to })),
)
</script>

<template>
  <UDashboardGroup>
    <UDashboardSidebar collapsible>
      <template #header>
        <span class="flex items-center gap-2 font-semibold">
          <UIcon
            name="i-lucide-cpu"
            class="size-5 text-primary"
          />
          {{ t('app.title') }}
        </span>
      </template>

      <UNavigationMenu
        :items="links"
        orientation="vertical"
      />

      <template #footer>
        <div class="flex w-full items-center justify-between">
          <UColorModeButton />
          <span
            v-if="version"
            class="text-xs text-muted"
          >v{{ version }}</span>
        </div>
      </template>
    </UDashboardSidebar>

    <slot />
  </UDashboardGroup>
</template>
