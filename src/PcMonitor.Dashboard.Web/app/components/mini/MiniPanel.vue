<script setup lang="ts">
import type { MiniReading } from '~/utils/mini-readings'

const props = defineProps<{
  readings: readonly MiniReading[]
  dot: 'success' | 'error'
  unseenIncidentId: number | null
}>()

const emit = defineEmits<{ open: [path: string] }>()

const { t, locale } = useI18n()

// Full class names so Tailwind finds them.
const valueClass = { success: 'text-highlighted', neutral: 'text-highlighted', warning: 'text-warning', error: 'text-error' } as const
</script>

<template>
  <div
    class="flex h-14 w-full cursor-pointer select-none items-center gap-3 border border-default bg-default/95 px-3 shadow-lg"
    role="button"
    :aria-label="t('mini.open')"
    @click="emit('open', props.unseenIncidentId === null ? '/' : `/incidents/${props.unseenIncidentId}`)"
  >
    <!-- The icon is the drag handle of the tray app's window (WebView2 non-client region). -->
    <img
      src="/icon.svg"
      alt=""
      class="size-5 shrink-0 [-webkit-app-region:drag]"
    >
    <span
      data-dot
      class="size-2 shrink-0 rounded-full"
      :class="props.dot === 'success' ? 'bg-success' : 'bg-error'"
    />
    <div
      v-for="reading in props.readings"
      :key="reading.role"
      :data-role="reading.role"
      class="flex flex-col leading-tight"
    >
      <span class="text-[10px] text-muted">{{ t(reading.labelKey) }}</span>
      <span
        data-value
        class="text-sm font-semibold tabular-nums"
        :class="valueClass[reading.status]"
      >{{ formatNumber(reading.value, locale, reading.digits) }}</span>
    </div>
    <UIcon
      v-if="props.unseenIncidentId !== null"
      data-unseen
      name="i-material-symbols-warning-outline-rounded"
      class="ms-auto size-4 shrink-0 text-error"
      :aria-label="t('mini.unseen')"
    />
  </div>
</template>
