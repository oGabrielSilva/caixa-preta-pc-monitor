<script setup lang="ts">
import type { RangeResponse } from '~/api/types'

const props = defineProps<{ name: string, value: number | null, range: RangeResponse }>()

const { locale } = useI18n()

// The band (safe range) fills 10 % – 90 % of the track; readings outside it stop at the ends.
const position = computed(() => {
  if (props.value === null) {
    return null
  }
  const ratio = (props.value - props.range.min) / (props.range.max - props.range.min)
  return Math.min(100, Math.max(0, 10 + ratio * 80))
})

// Full class names so Tailwind finds them.
const markerClass = computed(() => {
  if (props.value === null) {
    return 'bg-neutral-500'
  }
  return props.value >= props.range.min && props.value <= props.range.max ? 'bg-success' : 'bg-error'
})
</script>

<template>
  <div>
    <div class="mb-1 flex items-baseline justify-between text-sm">
      <span class="text-muted">{{ props.name }}</span>
      <span class="font-medium tabular-nums">{{ formatNumber(props.value, locale, 3) }} V</span>
    </div>
    <div class="relative h-2 rounded-full bg-elevated">
      <div class="absolute inset-y-0 left-[10%] right-[10%] rounded-full bg-success/25" />
      <div
        v-if="position !== null"
        data-marker
        class="absolute top-1/2 size-3 -translate-x-1/2 -translate-y-1/2 rounded-full ring-2 ring-default"
        :class="markerClass"
        :style="{ left: `${position}%` }"
      />
    </div>
    <div class="mt-0.5 flex justify-between text-[11px] text-dimmed tabular-nums">
      <span>{{ formatNumber(props.range.min, locale, 2) }}</span><span>{{ formatNumber(props.range.max, locale, 2) }}</span>
    </div>
  </div>
</template>
