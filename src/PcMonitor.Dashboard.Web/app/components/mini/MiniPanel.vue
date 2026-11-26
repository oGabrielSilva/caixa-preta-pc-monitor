<script setup lang="ts">
import type { MiniReading } from '~/utils/mini-readings'

const props = defineProps<{
  readings: readonly MiniReading[]
  dot: 'success' | 'error'
  unseenIncidentId: number | null
}>()

const emit = defineEmits<{ open: [path: string] }>()

const { t, locale } = useI18n()

const root = useTemplateRef<HTMLElement>('root')
useHostSize(root)

// Full class names so Tailwind finds them.
const valueClass = { success: 'text-highlighted', neutral: 'text-highlighted', warning: 'text-warning', error: 'text-error' } as const
</script>

<template>
  <div
    ref="root"
    class="flex h-10 w-max cursor-pointer select-none items-center gap-2 border border-default bg-default/95 ps-2 pe-0.5 shadow-lg"
    role="button"
    :aria-label="t('mini.open')"
    @wheel="scaleHostWithWheel"
    @click="emit('open', props.unseenIncidentId === null ? '/' : `/incidents/${props.unseenIncidentId}`)"
  >
    <!-- The icon is the drag handle of the tray app's window: pressing it asks the host to move the window. -->
    <img
      data-drag
      src="/icon.svg"
      alt=""
      class="size-4 shrink-0 cursor-move"
      draggable="false"
      @mousedown.left.prevent="startHostDrag"
      @click.stop
    >
    <span
      data-dot
      class="size-1.5 shrink-0 rounded-full"
      :class="props.dot === 'success' ? 'bg-success' : 'bg-error'"
    />
    <div
      v-for="reading in props.readings"
      :key="reading.role"
      :data-role="reading.role"
      class="flex flex-col leading-tight"
    >
      <span class="text-[9px] text-muted">{{ t(reading.labelKey) }}</span>
      <span
        data-value
        class="text-xs font-semibold tabular-nums"
        :class="valueClass[reading.status]"
      >{{ formatNumber(reading.value, locale, reading.digits) }}</span>
    </div>
    <UIcon
      v-if="props.unseenIncidentId !== null"
      data-unseen
      name="i-material-symbols-warning-outline-rounded"
      class="size-3.5 shrink-0 text-error"
      :aria-label="t('mini.unseen')"
    />
    <!-- Bottom-right grip: pressing it asks the host to resize the window; the content scales with it. -->
    <UIcon
      data-resize
      name="i-material-symbols-resize-rounded"
      class="size-3 shrink-0 cursor-nwse-resize self-end text-dimmed"
      :aria-label="t('mini.resize')"
      @mousedown.left.prevent="startHostResize"
      @click.stop
    />
  </div>
</template>
