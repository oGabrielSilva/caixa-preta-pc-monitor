<script setup lang="ts">
import { AUTHOR, RELEASES_PAGE, REPO_URL } from '~~/shared/utils/site'

const { t } = useI18n()
const localePath = useLocalePath()
const publicPath = usePublicPath()
</script>

<template>
  <footer class="border-t border-default">
    <div class="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-10 sm:px-6 md:flex-row md:items-center md:justify-between">
      <div class="flex items-center gap-3">
        <img
          :src="publicPath('/icon.svg')"
          alt=""
          width="24"
          height="24"
        >
        <p class="text-sm text-muted">
          <a
            :href="AUTHOR.url"
            class="hover:text-highlighted"
          >{{ t('footer.madeBy') }}</a>
        </p>
      </div>
      <ul class="flex flex-wrap gap-x-6 gap-y-2 text-sm">
        <li>
          <NuxtLink
            :to="localePath('releases')"
            class="text-muted hover:text-highlighted"
          >
            {{ t('footer.releases') }}
          </NuxtLink>
        </li>
        <li>
          <a
            :href="`${REPO_URL}/issues`"
            class="text-muted hover:text-highlighted"
          >{{ t('footer.issues') }}</a>
        </li>
        <li>
          <a
            :href="`${REPO_URL}/blob/main/LICENSE`"
            class="text-muted hover:text-highlighted"
          >{{ t('footer.license') }}</a>
        </li>
        <li>
          <a
            :href="RELEASES_PAGE"
            class="text-muted hover:text-highlighted"
          >GitHub</a>
        </li>
      </ul>
    </div>
  </footer>
</template>
