<script setup lang="ts">
import type { SessionResponse } from '~/api/types'

const props = defineProps<{ session: SessionResponse | null }>()

const { t } = useI18n()
</script>

<template>
  <UAlert
    v-if="props.session && props.session.sourceStatus !== 'Ok'"
    color="warning"
    variant="subtle"
    icon="i-lucide-cpu"
    :description="t(`live.source.${props.session.sourceStatus}`)"
  />
</template>
