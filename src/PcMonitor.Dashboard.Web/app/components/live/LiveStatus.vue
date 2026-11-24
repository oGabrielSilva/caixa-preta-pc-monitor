<script setup lang="ts">
import type { LiveResponse, LiveState } from '~/api/types'

const props = defineProps<{ state: LiveState, session: LiveResponse['session'] }>()

const { t } = useI18n()
const now = useClock(30_000)

const dot = computed(() => ({ Recording: 'bg-success', Stale: 'bg-warning', Stopped: 'bg-error', NoData: 'bg-neutral-500' })[props.state])
</script>

<template>
  <div class="flex items-center gap-2 text-sm text-muted">
    <span class="relative flex size-2.5">
      <span
        v-if="props.state === 'Recording'"
        class="absolute inline-flex size-full animate-ping rounded-full bg-success opacity-75"
      />
      <span
        class="relative inline-flex size-2.5 rounded-full"
        :class="dot"
      />
    </span>
    <span>{{ t(`live.state.${props.state}`) }}</span>
    <span
      v-if="props.session && props.state === 'Recording'"
      class="hidden sm:inline"
    >· {{ t('live.since', { duration: formatDuration(now - new Date(props.session.startedAt).getTime()) }) }}</span>
  </div>
</template>
