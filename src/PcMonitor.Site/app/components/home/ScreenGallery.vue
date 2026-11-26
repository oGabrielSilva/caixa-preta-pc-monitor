<script setup lang="ts">
import { SCREENS, screenSrc } from '~/utils/screens'

const { t } = useI18n()
const publicPath = usePublicPath()

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

      <UTabs
        :items="items"
        :unmount-on-hide="false"
        default-value="overview"
        variant="link"
        class="mt-10"
        :ui="{ list: 'overflow-x-auto', content: 'pt-6' }"
      >
        <template #screen="{ item }">
          <figure
            :data-screen="item.screen.id"
            class="flex flex-col gap-4"
          >
            <div class="flex justify-center border border-accented bg-elevated p-1.5">
              <img
                :src="publicPath(screenSrc(item.screen.id, item.screen.widths[0]))"
                :srcset="srcset(item.screen)"
                sizes="(min-width: 1280px) 1200px, 100vw"
                :alt="t(`gallery.screens.${item.screen.id}.alt`)"
                :width="item.screen.width"
                :height="item.screen.height"
                loading="lazy"
                class="block h-auto w-full"
                :class="item.screen.id === 'mini' ? 'max-w-[804px]' : ''"
              >
            </div>
            <figcaption class="flex flex-wrap items-center gap-3 text-toned">
              {{ t(`gallery.screens.${item.screen.id}.caption`) }}
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
