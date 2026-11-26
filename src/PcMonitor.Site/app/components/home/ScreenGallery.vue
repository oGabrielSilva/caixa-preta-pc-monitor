<script setup lang="ts">
import type { ScreenId } from '~/utils/screens'
import { SCREENS, screenSrc } from '~/utils/screens'

const { t } = useI18n()
const publicPath = usePublicPath()

const icons: Record<ScreenId, string> = {
  overview: 'i-material-symbols-monitoring-rounded',
  history: 'i-material-symbols-show-chart-rounded',
  incident: 'i-material-symbols-warning-outline-rounded',
  mini: 'i-material-symbols-picture-in-picture-alt-outline-rounded',
}

const items = computed(() => SCREENS.map(screen => ({
  label: t(`gallery.screens.${screen.id}.tab`),
  value: screen.id,
  slot: 'screen' as const,
  screen,
})))

const srcset = (id: (typeof SCREENS)[number]) => id.widths.map(width => `${publicPath(screenSrc(id.id, width))} ${width}w`).join(', ')
</script>

<template>
  <section
    id="telas"
    class="scroll-mt-20 border-t border-default"
  >
    <div class="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
        {{ t('gallery.title') }}
      </h2>
      <p class="mt-4 max-w-2xl text-lg text-toned">
        {{ t('gallery.lead') }}
      </p>

      <!-- Tabs on the left with what each screen shows (a row that wraps on phones), the screen on the right. -->
      <UTabs
        :items="items"
        :unmount-on-hide="false"
        default-value="overview"
        orientation="vertical"
        variant="link"
        class="mt-10"
        :ui="{
          root: 'flex-col items-stretch gap-6 lg:flex-row lg:gap-10',
          list: 'grid w-full grid-cols-2 gap-px border border-default bg-[var(--ui-border)] p-0 sm:grid-cols-4 lg:w-72 lg:shrink-0 lg:grid-cols-1',
          indicator: 'hidden',
          trigger: 'min-w-0 flex-col items-start gap-1 whitespace-normal bg-default px-4 py-3 text-left text-toned data-[state=active]:bg-elevated data-[state=active]:text-highlighted data-[state=active]:shadow-[inset_3px_0_0_var(--ui-primary)] lg:px-5 lg:py-4',
          content: 'min-w-0 flex-1',
        }"
      >
        <template #default="{ item }">
          <span class="flex items-center gap-2 font-bold">
            <UIcon
              :name="icons[item.screen.id]"
              class="size-5 shrink-0 text-primary"
            />
            {{ item.label }}
          </span>
          <span class="hidden whitespace-normal break-words text-sm font-normal leading-snug text-muted lg:block">
            {{ t(`gallery.screens.${item.screen.id}.caption`) }}
          </span>
        </template>

        <template #screen="{ item }">
          <figure
            :data-screen="item.screen.id"
            class="flex flex-col gap-4"
          >
            <div class="flex justify-center border border-accented bg-elevated p-1.5">
              <img
                :src="publicPath(screenSrc(item.screen.id, item.screen.widths[0]))"
                :srcset="srcset(item.screen)"
                sizes="(min-width: 1280px) 900px, 100vw"
                :alt="t(`gallery.screens.${item.screen.id}.alt`)"
                :width="item.screen.width"
                :height="item.screen.height"
                loading="lazy"
                class="block h-auto w-full"
                :class="item.screen.id === 'mini' ? 'max-w-[804px]' : ''"
              >
            </div>
            <figcaption class="flex flex-wrap items-center gap-3 text-toned">
              <span class="lg:hidden">{{ t(`gallery.screens.${item.screen.id}.caption`) }}</span>
              <UBadge
                v-if="item.screen.sample"
                color="warning"
                variant="outline"
                icon="i-material-symbols-science-outline-rounded"
                :label="t('gallery.sample')"
              />
            </figcaption>
          </figure>
        </template>
      </UTabs>
    </div>
  </section>
</template>
