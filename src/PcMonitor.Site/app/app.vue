<script setup lang="ts">
import { en, pt_br } from '@nuxt/ui/locale'

const { locale } = useI18n()
const i18nHead = useLocaleHead({ seo: true })

useHead({
  htmlAttrs: { lang: () => i18nHead.value.htmlAttrs?.lang },
})
</script>

<template>
  <UApp :locale="locale === 'en' ? en : pt_br">
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>
