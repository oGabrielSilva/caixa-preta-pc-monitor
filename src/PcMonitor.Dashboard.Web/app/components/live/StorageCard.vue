<script setup lang="ts">
import type { DiskReading } from '~/utils/storage-readings'

const props = defineProps<{ disks: readonly DiskReading[] }>()

const { t, locale } = useI18n()

// Full class names so Tailwind finds them.
const tone = { success: 'text-highlighted', neutral: 'text-highlighted', warning: 'text-warning', error: 'text-error' } as const
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('live.storage.title') }}
      </h3>
    </template>
    <p
      v-if="props.disks.length === 0"
      class="text-sm text-muted"
    >
      {{ t('live.storage.none') }}
    </p>
    <div
      v-for="disk in props.disks"
      :key="disk.disk"
      class="grid grid-cols-[auto_1fr_1fr_1fr] items-baseline gap-x-4 gap-y-1 text-sm"
    >
      <span class="font-medium">{{ t('live.storage.disk', { number: disk.disk + 1 }) }}</span>
      <span>
        <span class="text-muted">{{ t('live.storage.temperature') }} </span>
        <span
          class="font-medium tabular-nums"
          :class="tone[disk.temperatureStatus]"
        >{{ formatNumber(disk.temperature, locale, 0) }} °C</span>
      </span>
      <span>
        <span class="text-muted">{{ t('live.storage.life') }} </span>
        <span
          class="font-medium tabular-nums"
          :class="tone[disk.lifeStatus]"
        >{{ formatNumber(disk.life, locale, 0) }} %</span>
      </span>
      <span>
        <span class="text-muted">{{ t('live.storage.used') }} </span>
        <span class="font-medium tabular-nums">{{ formatNumber(disk.usedSpace, locale, 0) }} %</span>
      </span>
    </div>
  </UCard>
</template>
