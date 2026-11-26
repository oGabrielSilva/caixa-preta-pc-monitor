<script setup lang="ts">
import { siteLocales } from '~~/i18n/site-locales'
import { REPO_URL } from '~~/shared/utils/site'

const { t, locale } = useI18n()
const localePath = useLocalePath()
const switchLocalePath = useSwitchLocalePath()
const publicPath = usePublicPath()

// Each language is its own page (/ and /en/): choosing one opens the same page in that language.
const languages = siteLocales.map(({ code, name }) => ({ label: name, value: code }))
function changeLanguage(code: unknown) {
  const target = siteLocales.find(l => l.code === code)
  if (target && target.code !== locale.value) {
    void navigateTo(switchLocalePath(target.code))
  }
}
const home = computed(() => localePath('/'))
const links = computed(() => [
  { label: t('nav.how'), to: `${home.value}#como-funciona` },
  { label: t('nav.features'), to: `${home.value}#recursos` },
  { label: t('nav.engineering'), to: `${home.value}#engenharia` },
  { label: t('nav.releases'), to: localePath('releases') },
])
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-default bg-default/90 backdrop-blur">
    <a
      href="#conteudo"
      class="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-3 focus:bg-primary focus:px-3 focus:py-2 focus:text-inverted"
    >{{ t('nav.skip') }}</a>
    <div class="mx-auto flex h-16 max-w-7xl items-center gap-3 px-4 sm:gap-6 sm:px-6">
      <NuxtLink
        :to="home"
        class="flex shrink-0 items-center gap-2.5 font-bold text-highlighted"
        :aria-label="t('nav.home')"
      >
        <img
          :src="publicPath('/icon.svg')"
          alt=""
          width="28"
          height="28"
        >
        <span class="whitespace-nowrap text-lg">Caixa-Preta</span>
      </NuxtLink>

      <nav
        class="hidden flex-1 items-center gap-1 md:flex"
        :aria-label="t('nav.menu')"
      >
        <UButton
          v-for="link in links"
          :key="link.to"
          :to="link.to"
          variant="ghost"
          color="neutral"
        >
          {{ link.label }}
        </UButton>
      </nav>

      <div class="ml-auto flex items-center gap-1">
        <USelect
          data-language-select
          :model-value="locale"
          :items="languages"
          :aria-label="t('nav.language')"
          icon="i-material-symbols-translate-rounded"
          variant="ghost"
          color="neutral"
          class="w-auto"
          :ui="{ leadingIcon: 'hidden sm:inline-flex', base: 'max-sm:ps-2' }"
          @update:model-value="changeLanguage"
        />
        <UColorModeButton
          color="neutral"
          variant="ghost"
          :aria-label="t('theme')"
        />
        <UButton
          :href="REPO_URL"
          variant="outline"
          color="neutral"
          icon="i-material-symbols-code-rounded"
          class="hidden sm:inline-flex"
        >
          {{ t('nav.github') }}
        </UButton>
      </div>
    </div>
  </header>
</template>
