<script setup lang="ts">
const { t } = useI18n()
const publicPath = usePublicPath()

const readings = ['°C', '%', 'W', '+12 V', '+5 V', '+3,3 V', 'RPM', 'RAM', 'NVMe']
const scoreboard = [
  { key: 'Fonte', en: 'Power supply', score: 3 },
  { key: 'Placa-mãe', en: 'Motherboard', score: 2 },
  { key: 'Superaquecimento', en: 'Overheating', score: -1 },
  { key: 'Software', en: 'Software', score: -1 },
]
const { locale } = useI18n()
</script>

<template>
  <section
    id="recursos"
    class="scroll-mt-20 border-t border-default bg-muted/40"
  >
    <div class="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
        {{ t('features.title') }}
      </h2>
      <p class="mt-4 max-w-2xl text-lg text-toned">
        {{ t('features.lead') }}
      </p>

      <div class="mt-12 grid gap-px border border-default bg-[var(--ui-border)] md:grid-cols-6">
        <article class="flex flex-col gap-6 bg-default p-6 sm:p-8 md:col-span-4">
          <UIcon
            name="i-material-symbols-database-outline"
            class="size-8 text-primary"
          />
          <div>
            <h3 class="text-2xl font-bold text-highlighted">
              {{ t('features.recording.title') }}
            </h3>
            <p class="mt-2 max-w-xl text-toned">
              {{ t('features.recording.text') }}
            </p>
          </div>
          <ul class="mt-auto flex flex-wrap gap-2">
            <li
              v-for="reading in readings"
              :key="reading"
              class="border border-accented px-2.5 py-1 text-sm font-medium text-default tabular-nums"
            >
              {{ locale === 'en' ? reading.replace(',', '.') : reading }}
            </li>
          </ul>
        </article>

        <article class="flex flex-col gap-4 bg-default p-6 sm:p-8 md:col-span-2">
          <UIcon
            name="i-material-symbols-power-settings-new-rounded"
            class="size-8 text-error"
          />
          <h3 class="text-xl font-bold text-highlighted">
            {{ t('features.detection.title') }}
          </h3>
          <p class="text-toned">
            {{ t('features.detection.text') }}
          </p>
        </article>

        <article class="flex flex-col gap-4 bg-default p-6 sm:p-8 md:col-span-3">
          <UIcon
            name="i-material-symbols-fact-check-outline-rounded"
            class="size-8 text-primary"
          />
          <h3 class="text-xl font-bold text-highlighted">
            {{ t('features.diagnosis.title') }}
          </h3>
          <p class="text-toned">
            {{ t('features.diagnosis.text') }}
          </p>
          <ul
            class="mt-2 flex flex-col gap-2"
            aria-hidden="true"
          >
            <li
              v-for="row in scoreboard"
              :key="row.key"
              class="grid grid-cols-[9.5rem_1fr_2rem] items-center gap-3 text-sm"
            >
              <span class="truncate text-toned">{{ locale === 'en' ? row.en : row.key }}</span>
              <span class="relative h-1.5 bg-accented">
                <span
                  class="absolute inset-y-0"
                  :class="row.score > 0 ? 'left-1/2 bg-primary' : 'right-1/2 bg-error/70'"
                  :style="{ width: `${Math.abs(row.score) * 16}%` }"
                />
              </span>
              <span class="text-right font-bold tabular-nums text-highlighted">{{ row.score > 0 ? `+${row.score}` : `−${-row.score}` }}</span>
            </li>
          </ul>
        </article>

        <article class="flex flex-col gap-4 bg-default p-6 sm:p-8 md:col-span-3">
          <UIcon
            name="i-material-symbols-picture-in-picture-alt-outline-rounded"
            class="size-8 text-primary"
          />
          <h3 class="text-xl font-bold text-highlighted">
            {{ t('features.mini.title') }}
          </h3>
          <p class="text-toned">
            {{ t('features.mini.text') }}
          </p>
          <img
            :src="publicPath('/screens/mini-804.webp')"
            :srcset="`${publicPath('/screens/mini-402.webp')} 402w, ${publicPath('/screens/mini-804.webp')} 804w`"
            sizes="(min-width: 768px) 400px, 90vw"
            :alt="t('gallery.screens.mini.alt')"
            width="804"
            height="120"
            loading="lazy"
            class="mt-auto h-auto w-full max-w-[402px] border border-accented"
          >
        </article>

        <article class="flex flex-col gap-4 bg-default p-6 sm:p-8 md:col-span-3">
          <UIcon
            name="i-material-symbols-ios-share-rounded"
            class="size-8 text-primary"
          />
          <h3 class="text-xl font-bold text-highlighted">
            {{ t('features.export.title') }}
          </h3>
          <p class="text-toned">
            {{ t('features.export.text') }}
          </p>
        </article>

        <article class="flex flex-col gap-4 bg-default p-6 sm:p-8 md:col-span-3">
          <UIcon
            name="i-material-symbols-shield-lock-outline-rounded"
            class="size-8 text-primary"
          />
          <h3 class="text-xl font-bold text-highlighted">
            {{ t('features.privacy.title') }}
          </h3>
          <p class="text-toned">
            {{ t('features.privacy.text') }}
          </p>
        </article>
      </div>
    </div>
  </section>
</template>
