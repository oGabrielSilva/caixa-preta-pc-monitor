<script setup lang="ts">
import type { Release } from '~~/shared/utils/releases'
import { downloadHref, latestStable } from '~~/shared/utils/releases'
import { megabytes } from '~~/shared/utils/seo'

const props = defineProps<{ releases: Release[] }>()
const { t } = useI18n()

const release = computed(() => latestStable(props.releases))
const href = computed(() => downloadHref(props.releases))
</script>

<template>
  <div class="flex flex-col gap-2">
    <UButton
      :href="href"
      size="xl"
      :icon="release ? 'i-material-symbols-download-rounded' : 'i-material-symbols-open-in-new-rounded'"
      class="w-fit px-6 py-3.5 text-base font-bold"
      :rel="release ? undefined : 'noopener'"
    >
      {{ release ? t('hero.download') : t('hero.soon') }}
    </UButton>
    <p class="text-sm text-muted tabular-nums">
      {{ release?.installer
        ? t('hero.downloadMeta', { version: release.version, size: megabytes(release.installer.size) })
        : t('hero.soonMeta') }}
    </p>
  </div>
</template>
