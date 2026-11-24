<script setup lang="ts">
const { locale, t } = useI18n()

useHead({
  htmlAttrs: { lang: locale },
  title: () => t('app.title'),
})
</script>

<template>
  <UApp :locale="uiLocaleFor(locale)">
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>
