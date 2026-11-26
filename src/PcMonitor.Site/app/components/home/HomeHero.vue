<script setup lang="ts">
import DownloadButton from '~/components/shared/DownloadButton.vue'
import PulseLine from '~/components/shared/PulseLine.vue'
import type { Release } from '~~/shared/utils/releases'
import { REPO_URL } from '~~/shared/utils/site'

defineProps<{ releases: Release[] }>()
const { t } = useI18n()

// The product name never breaks at its hyphen ("Caixa-" / "Preta" on narrow screens).
const BRAND = 'Caixa-Preta'
const answerParts = computed(() => t('hero.titleAnswer').split(new RegExp(`(${BRAND})`)).filter(Boolean))
const publicPath = usePublicPath()
</script>

<template>
  <section class="relative overflow-hidden">
    <div class="mx-auto grid max-w-7xl items-center gap-12 px-4 pt-14 pb-6 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] lg:pt-24">
      <div class="flex flex-col gap-7">
        <h1 class="text-[2.15rem] leading-[1.05] font-extrabold tracking-[-0.03em] text-highlighted sm:text-6xl xl:text-7xl">
          <span class="block text-balance">{{ t('hero.titleQuestion') }}</span>
          <span class="mt-2 block text-balance text-toned">
            <template
              v-for="(part, index) in answerParts"
              :key="index"
            >
              <span
                v-if="part === BRAND"
                class="whitespace-nowrap"
              >{{ part }}</span>
              <template v-else>{{ part }}</template>
            </template>
          </span>
        </h1>
        <p class="max-w-[34rem] text-lg leading-relaxed text-toned">
          {{ t('hero.lead') }}
        </p>
        <div class="flex flex-wrap items-start gap-4">
          <DownloadButton :releases="releases" />
          <UButton
            :href="REPO_URL"
            size="xl"
            variant="outline"
            color="neutral"
            icon="i-material-symbols-code-rounded"
            class="px-5 py-3.5 text-base"
          >
            {{ t('hero.source') }}
          </UButton>
        </div>
        <p class="flex items-center gap-2 text-sm text-muted">
          <UIcon
            name="i-material-symbols-lock-outline-rounded"
            class="size-4 text-primary"
          />
          {{ t('hero.free') }}
        </p>
      </div>

      <div class="relative pb-10 lg:pb-14">
        <div class="border border-accented bg-elevated p-1.5 shadow-[0_0_0_1px_var(--ui-bg),0_30px_80px_-30px_color-mix(in_oklab,var(--ui-primary)_45%,transparent)]">
          <img
            :src="publicPath('/screens/overview-1600.webp')"
            :srcset="`${publicPath('/screens/overview-800.webp')} 800w, ${publicPath('/screens/overview-1600.webp')} 1600w`"
            sizes="(min-width: 1024px) 50vw, 100vw"
            :alt="t('hero.screenAlt')"
            width="1600"
            height="1000"
            fetchpriority="high"
            class="block h-auto w-full"
          >
        </div>
        <img
          :src="publicPath('/screens/mini-804.webp')"
          :srcset="`${publicPath('/screens/mini-402.webp')} 402w, ${publicPath('/screens/mini-804.webp')} 804w`"
          sizes="(min-width: 640px) 300px, 70vw"
          :alt="t('hero.miniAlt')"
          width="804"
          height="120"
          class="absolute bottom-0 left-4 h-auto w-[70%] border border-primary/60 shadow-2xl sm:-left-6 sm:w-[300px]"
        >
      </div>
    </div>
    <PulseLine class="mt-2" />
  </section>
</template>
