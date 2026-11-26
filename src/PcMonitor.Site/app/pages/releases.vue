<script setup lang="ts">
import { releases } from '#releases'
import ReleaseList from '~/components/changelog/ReleaseList.vue'
import { breadcrumbJsonLd } from '~~/shared/utils/seo'

const { t } = useI18n()
const localePath = useLocalePath()

usePageSeo({
  titleKey: 'meta.releases.title',
  descriptionKey: 'meta.releases.description',
  jsonLd: () => [breadcrumbJsonLd([
    { name: t('releases.home'), path: localePath('/') },
    { name: t('releases.title'), path: localePath('releases') },
  ])],
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-16 sm:px-6 lg:py-24">
    <h1 class="text-4xl font-extrabold tracking-tight text-highlighted sm:text-6xl">
      {{ t('releases.title') }}
    </h1>
    <p class="mt-4 max-w-2xl text-lg text-toned">
      {{ t('releases.lead') }}
    </p>
    <div class="mt-14">
      <ReleaseList :releases="releases" />
    </div>
  </div>
</template>
