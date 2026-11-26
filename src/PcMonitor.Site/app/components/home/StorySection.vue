<script setup lang="ts">
import { AUTHOR } from '~~/shared/utils/site'

const { t } = useI18n()
</script>

<template>
  <section class="border-t border-default">
    <div class="mx-auto max-w-4xl px-4 py-20 sm:px-6 lg:py-28">
      <h2 class="text-lg font-bold text-primary">
        {{ t('story.title') }}
      </h2>
      <figure class="mt-6">
        <blockquote class="text-balance text-3xl leading-snug font-bold tracking-tight text-highlighted sm:text-4xl">
          <p>{{ t('story.text') }}</p>
        </blockquote>
        <figcaption class="mt-6 text-toned">
          <a
            :href="AUTHOR.url"
            class="hover:text-highlighted"
          >{{ t('story.author') }}</a>
        </figcaption>
      </figure>
    </div>
  </section>
</template>
