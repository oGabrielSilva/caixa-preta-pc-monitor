<script setup lang="ts">
import ReleaseNotes from '~/components/changelog/ReleaseNotes.vue'
import type { Release } from '~~/shared/utils/releases'
import { latestStable } from '~~/shared/utils/releases'
import { megabytes } from '~~/shared/utils/seo'
import { RELEASES_PAGE } from '~~/shared/utils/site'

const props = defineProps<{ releases: Release[] }>()
const { t, locale } = useI18n()
const date = (iso: string) => new Intl.DateTimeFormat(locale.value, { dateStyle: 'long', timeZone: 'UTC' }).format(new Date(iso))

const latest = computed(() => latestStable(props.releases)?.version)
const badge = (release: Release) => release.prerelease
  ? { label: t('releases.prerelease'), color: 'warning' as const, variant: 'outline' as const }
  : release.version === latest.value
    ? { label: t('releases.latest'), color: 'primary' as const, variant: 'solid' as const }
    : undefined
</script>

<template>
  <ol
    v-if="releases.length"
    class="flex flex-col"
  >
    <li
      v-for="release in releases"
      :key="release.tag"
      :data-release="release.version"
      class="grid gap-4 border-t border-default py-10 md:grid-cols-[11rem_minmax(0,1fr)] md:gap-8"
    >
      <div class="flex flex-row flex-wrap items-center gap-3 md:flex-col md:items-start">
        <time
          :datetime="release.publishedAt"
          class="text-sm text-muted tabular-nums"
        >{{ date(release.publishedAt) }}</time>
        <UBadge
          v-if="badge(release)"
          v-bind="badge(release)"
        />
      </div>
      <article>
        <h2 class="text-2xl font-extrabold tracking-tight text-highlighted">
          Caixa-Preta {{ release.version }}
        </h2>
        <ReleaseNotes
          :notes="release.notes"
          class="mt-3"
        />
        <div class="mt-6 flex flex-wrap items-center gap-3">
          <UButton
            v-if="release.installer"
            :href="release.installer.url"
            icon="i-material-symbols-download-rounded"
            :variant="release.version === latest ? 'solid' : 'outline'"
            :color="release.version === latest ? 'primary' : 'neutral'"
          >
            {{ t('releases.download', { size: megabytes(release.installer.size) }) }}
          </UButton>
          <p
            v-else
            class="text-sm text-muted"
          >
            {{ t('releases.noInstaller') }}
          </p>
          <UButton
            v-if="release.checksum"
            :href="release.checksum.url"
            variant="ghost"
            color="neutral"
            icon="i-material-symbols-verified-outline-rounded"
          >
            {{ t('releases.checksum') }}
          </UButton>
          <UButton
            :href="release.htmlUrl"
            variant="ghost"
            color="neutral"
            icon="i-material-symbols-open-in-new-rounded"
          >
            {{ t('releases.github') }}
          </UButton>
        </div>
      </article>
    </li>
  </ol>

  <div
    v-else
    class="border border-dashed border-accented p-8 sm:p-12"
  >
    <UIcon
      name="i-material-symbols-hourglass-top-rounded"
      class="size-8 text-primary"
    />
    <h2 class="mt-4 text-2xl font-extrabold tracking-tight text-highlighted">
      {{ t('releases.emptyTitle') }}
    </h2>
    <p class="mt-2 max-w-xl text-toned">
      {{ t('releases.emptyText') }}
    </p>
    <UButton
      :href="RELEASES_PAGE"
      class="mt-6"
      icon="i-material-symbols-open-in-new-rounded"
    >
      {{ t('releases.emptyButton') }}
    </UButton>
  </div>
</template>
