<script setup lang="ts">
import { releases } from '#releases'
import { latestStable } from '~~/shared/utils/releases'
import { faqPageJsonLd, softwareApplicationJsonLd, websiteJsonLd } from '~~/shared/utils/seo'

const { t, locale } = useI18n()

usePageSeo({
  titleKey: 'meta.home.title',
  descriptionKey: 'meta.home.description',
  jsonLd: () => [
    websiteJsonLd(locale.value),
    softwareApplicationJsonLd({ description: t('meta.home.description'), locale: locale.value, release: latestStable(releases), screenshots: [] }),
    faqPageJsonLd(FAQ_IDS.map(id => ({ question: t(faqKeys(id).question), answer: t(faqKeys(id).answer) }))),
  ],
})
</script>

<template>
  <main>
    <h1>{{ t('hero.title') }}</h1>
  </main>
</template>
