<script setup lang="ts">
// "Inset" layout of the Nuxt UI Sidebar: the navigation sits on the background and each page is a card beside it.
// The toggle lives in each page's title bar (shared/Page.vue).
const { t } = useI18n()
const { version } = useHealth()
const open = useSidebar()

const links = computed(() =>
  navigationItems.map(item => ({ label: t(item.labelKey), icon: item.icon, to: item.to })),
)
</script>

<template>
  <div class="flex h-svh bg-muted">
    <USidebar
      v-model:open="open"
      variant="inset"
      collapsible="icon"
      :ui="{ container: 'h-full' }"
    >
      <template #header="{ state }">
        <span class="flex items-center gap-2 overflow-hidden font-semibold">
          <img
            src="/icon.svg"
            alt=""
            class="size-6 shrink-0"
          >
          <span v-if="state === 'expanded'">{{ t('app.title') }}</span>
        </span>
      </template>

      <template #default="{ state }">
        <UNavigationMenu
          :items="links"
          orientation="vertical"
          :collapsed="state === 'collapsed'"
          :ui="{ link: 'p-1.5 overflow-hidden' }"
        />
      </template>

      <template #footer="{ state }">
        <div
          class="flex w-full items-center gap-2"
          :class="state === 'expanded' ? 'justify-between' : 'justify-center'"
        >
          <UColorModeButton />
          <span
            v-if="version && state === 'expanded'"
            class="text-xs text-muted"
          >v{{ version }}</span>
        </div>
      </template>
    </USidebar>

    <main
      class="flex min-w-0 flex-1 flex-col overflow-y-auto bg-default peer-data-[variant=inset]:m-4 lg:peer-data-[variant=inset]:not-peer-data-[collapsible=offcanvas]:ms-0 peer-data-[variant=inset]:rounded-[calc(var(--ui-radius)*2)] peer-data-[variant=inset]:shadow-sm peer-data-[variant=inset]:ring peer-data-[variant=inset]:ring-default"
    >
      <slot />
    </main>
  </div>
</template>
