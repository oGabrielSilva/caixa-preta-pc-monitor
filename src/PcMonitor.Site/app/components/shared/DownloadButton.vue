<script setup lang="ts">
import type { Release } from '~~/shared/utils/releases'
import { latestStable } from '~~/shared/utils/releases'
import { megabytes } from '~~/shared/utils/seo'

// Leads to the releases page (notes, checksum and older versions) instead of starting the download at once.
const props = defineProps<{ releases: Release[] }>()
const { t } = useI18n()
const localePath = useLocalePath()

const release = computed(() => latestStable(props.releases))
</script>

<template>
  <div class="flex flex-col gap-2">
    <UButton
      :to="localePath('releases')"
      size="xl"
      :icon="release ? 'i-material-symbols-download-rounded' : 'i-material-symbols-schedule-rounded'"
      class="w-fit px-6 py-3.5 text-base font-bold"
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
