<script setup lang="ts">
import { REPO_URL } from '~~/shared/utils/site'

const { t } = useI18n()

const processes = [
  { key: 'collector', icon: 'i-material-symbols-sensors-rounded' },
  { key: 'database', icon: 'i-material-symbols-database-outline' },
  { key: 'dashboard', icon: 'i-material-symbols-monitoring-rounded' },
  { key: 'tray', icon: 'i-material-symbols-desktop-windows-outline-rounded' },
] as const
const facts = ['stack', 'layers', 'tests', 'adrs', 'installer'] as const
</script>

<template>
  <section
    id="engenharia"
    class="scroll-mt-20 border-t border-default bg-muted/40"
  >
    <div class="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
        {{ t('engineering.title') }}
      </h2>
      <p class="mt-4 max-w-2xl text-lg text-toned">
        {{ t('engineering.lead') }}
      </p>

      <ol
        class="mt-12 grid gap-px border border-default bg-[var(--ui-border)] md:grid-cols-4"
        :aria-label="t('engineering.diagramLabel')"
      >
        <li
          v-for="(process, index) in processes"
          :key="process.key"
          class="relative flex flex-col gap-3 bg-default p-6"
        >
          <UIcon
            :name="process.icon"
            class="size-7"
            :class="process.key === 'database' ? 'text-toned' : 'text-primary'"
          />
          <h3 class="text-lg font-bold text-highlighted">
            {{ t(`engineering.processes.${process.key}.name`) }}
          </h3>
          <p class="text-toned">
            {{ t(`engineering.processes.${process.key}.role`) }}
          </p>
          <p
            v-if="process.key !== 'database'"
            class="mt-auto text-sm text-muted"
          >
            {{ t(`engineering.processes.${process.key}.privilege`) }}
          </p>
          <span
            v-if="index < processes.length - 1"
            class="absolute -right-3.5 top-7 z-10 hidden size-7 items-center justify-center border border-default bg-default md:flex"
            aria-hidden="true"
          >
            <UIcon
              name="i-material-symbols-arrow-forward-rounded"
              class="size-5 text-primary"
            />
          </span>
        </li>
      </ol>

      <div class="mt-12 grid gap-10 md:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)] md:items-end">
        <ul class="flex flex-col gap-3">
          <li
            v-for="fact in facts"
            :key="fact"
            class="flex gap-3 text-default"
          >
            <UIcon
              name="i-material-symbols-check-rounded"
              class="mt-0.5 size-5 shrink-0 text-primary"
            />
            {{ t(`engineering.facts.${fact}`) }}
          </li>
        </ul>
        <div class="flex flex-wrap gap-3 md:justify-end">
          <UButton
            :href="REPO_URL"
            variant="outline"
            color="neutral"
            icon="i-material-symbols-code-rounded"
          >
            {{ t('engineering.code') }}
          </UButton>
          <UButton
            :href="`${REPO_URL}/tree/main/docs/adr`"
            variant="outline"
            color="neutral"
            icon="i-material-symbols-description-outline-rounded"
          >
            {{ t('engineering.adrs') }}
          </UButton>
        </div>
      </div>
    </div>
  </section>
</template>
