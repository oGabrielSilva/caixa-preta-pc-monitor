<script setup lang="ts">
import { releases } from '#releases'
// Explicit imports: the Vitest Nuxt runtime does not resolve un-prefixed component names (components.pathPrefix).
import EngineeringSection from '~/components/home/EngineeringSection.vue'
import FaqSection from '~/components/home/FaqSection.vue'
import FeatureBento from '~/components/home/FeatureBento.vue'
import FlightRecorder from '~/components/home/FlightRecorder.vue'
import HomeHero from '~/components/home/HomeHero.vue'
import ScreenGallery from '~/components/home/ScreenGallery.vue'
import StorySection from '~/components/home/StorySection.vue'
import DownloadButton from '~/components/shared/DownloadButton.vue'
import { latestStable } from '~~/shared/utils/releases'
import { absoluteUrl, faqPageJsonLd, softwareApplicationJsonLd, websiteJsonLd } from '~~/shared/utils/seo'

const { t, locale } = useI18n()

usePageSeo({
  titleKey: 'meta.home.title',
  descriptionKey: 'meta.home.description',
  jsonLd: () => [
    websiteJsonLd(locale.value),
    softwareApplicationJsonLd({
      description: t('meta.home.description'),
      locale: locale.value,
      release: latestStable(releases),
      screenshots: SCREENS.map(screen => absoluteUrl(screenSrc(screen.id, screen.widths[0]))),
    }),
    faqPageJsonLd(FAQ_IDS.map(id => ({ question: t(faqKeys(id).question), answer: t(faqKeys(id).answer) }))),
  ],
})
</script>

<template>
  <div>
    <HomeHero :releases="releases" />
    <FlightRecorder />
    <FeatureBento />
    <ScreenGallery />
    <EngineeringSection />
    <StorySection />
    <FaqSection />
    <section class="border-t border-default">
      <div class="mx-auto flex max-w-7xl flex-col gap-8 px-4 py-20 sm:px-6 md:flex-row md:items-end md:justify-between lg:py-28">
        <div class="max-w-2xl">
          <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
            {{ t('cta.title') }}
          </h2>
          <p class="mt-4 text-lg text-toned">
            {{ t('cta.text') }}
          </p>
        </div>
        <DownloadButton :releases="releases" />
      </div>
    </section>
  </div>
</template>
