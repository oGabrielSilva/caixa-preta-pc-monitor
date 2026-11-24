<script setup lang="ts">
import type { SensorRole, TelemetryResponse } from '~/api/types'
import type { SeriesPoint } from '~/utils/live-buffer'

const props = defineProps<{ telemetry: TelemetryResponse | null }>()

const { t, locale } = useI18n()

function series(role: SensorRole): SeriesPoint[] {
  return props.telemetry?.series.find(s => s.role === role)?.points.map(p => ({ t: p.t, v: p.v })) ?? []
}

const rail = computed(() => props.telemetry?.series.find(s => s.role === 'Rail12V'))
const cpu = computed(() => ({ temperature: series('CpuTemperature'), load: series('CpuLoad') }))
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
        <h3 class="font-medium">
          {{ t('incident.telemetry.title') }}
        </h3>
        <span
          v-if="props.telemetry"
          class="text-xs text-dimmed"
        >{{ t('incident.telemetry.perSecond') }}</span>
      </div>
    </template>
    <p
      v-if="!props.telemetry"
      class="text-sm text-muted"
    >
      {{ t('incident.telemetry.none') }}
    </p>
    <template v-else>
      <UAlert
        v-if="!props.telemetry.atFailure"
        class="mb-4"
        color="warning"
        variant="subtle"
        icon="i-lucide-clock-alert"
        :description="t('incident.telemetry.notAtFailure')"
      />
      <div class="grid gap-6 lg:grid-cols-2">
        <div v-if="rail">
          <p class="mb-2 text-sm text-muted">
            {{ rail.range
              ? t('incident.telemetry.rail', { min: formatNumber(rail.range.min, locale, 2), max: formatNumber(rail.range.max, locale, 2) })
              : t('roles.Rail12V') }}
          </p>
          <SharedTimeSeriesChart
            :series="{ rail: series('Rail12V') }"
            :categories="[{ key: 'rail', label: t('roles.Rail12V'), color: 'var(--color-rose-400)' }]"
            :step-ms="1000"
            :height="200"
          />
        </div>
        <div>
          <p class="mb-2 text-sm text-muted">
            {{ t('incident.telemetry.cpu') }}
          </p>
          <SharedTimeSeriesChart
            :series="cpu"
            :categories="[
              { key: 'temperature', label: t('live.trend.temperature'), color: 'var(--color-amber-400)' },
              { key: 'load', label: t('live.trend.load'), color: 'var(--color-sky-400)' },
            ]"
            :step-ms="1000"
            :height="200"
          />
        </div>
      </div>
    </template>
  </UCard>
</template>
