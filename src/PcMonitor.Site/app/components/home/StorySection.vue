<script setup lang="ts">
import CatFall from '~/components/home/CatFall.vue'
import { AUTHOR } from '~~/shared/utils/site'

const { t } = useI18n()
</script>

<template>
  <section
    id="historia"
    class="scroll-mt-20 border-t border-default"
  >
    <div class="mx-auto grid max-w-7xl items-center gap-12 px-4 py-20 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] lg:py-28">
      <div>
        <h2 class="text-balance text-3xl leading-tight font-extrabold tracking-tight text-highlighted sm:text-5xl">
          {{ t('story.quote') }}
        </h2>
        <p class="mt-6 max-w-[58ch] text-lg leading-relaxed text-toned">
          {{ t('story.text') }}
        </p>
        <p class="mt-6 text-sm text-muted">
          <a
            :href="AUTHOR.url"
            class="hover:text-highlighted"
          >{{ t('story.author') }}</a>
        </p>
      </div>
      <figure class="border border-default bg-elevated/40 p-4 sm:p-6">
        <CatFall />
      </figure>
    </div>
  </section>
</template>
