<script setup lang="ts">
import type { HypothesisScore } from '~/api/types'

const props = defineProps<{ ranking: readonly HypothesisScore[] }>()

const { t } = useI18n()

const largest = computed(() => Math.max(1, ...props.ranking.map(r => Math.abs(r.score))))
</script>

<template>
  <div class="space-y-3">
    <div
      v-for="row in props.ranking"
      :key="row.hypothesis ?? 'none'"
      class="grid grid-cols-[minmax(7rem,10rem)_1fr_2.5rem] items-center gap-3 text-sm"
    >
      <span class="text-muted">{{ t(`hypotheses.${row.hypothesis}`) }}</span>
      <div class="relative h-2 rounded-full bg-elevated">
        <div
          :data-direction="row.score > 0 ? 'for' : 'against'"
          class="absolute inset-y-0 rounded-full"
          :class="row.score > 0 ? 'left-1/2 bg-primary' : 'right-1/2 bg-error/60'"
          :style="{ width: `${(Math.abs(row.score) / largest) * 50}%` }"
        />
        <div class="absolute -inset-y-[3px] left-1/2 w-px bg-accented" />
      </div>
      <span class="text-right font-medium tabular-nums">{{ row.score > 0 ? '+' : '' }}{{ row.score }}</span>
    </div>
  </div>
</template>
