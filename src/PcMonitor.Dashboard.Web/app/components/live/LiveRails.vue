<script setup lang="ts">
import type { LiveBuffer } from '~/utils/live-buffer'

const props = defineProps<{ buffer: LiveBuffer }>()

const { t, locale } = useI18n()

const rails = computed(() => supplyRails(props.buffer.sensors)
  .map(s => ({ key: s.key, name: t(`roles.${s.role}`), value: latestValue(props.buffer, s.key), range: s.range! })))

const others = computed(() => secondaryReadings(props.buffer.sensors, key => latestValue(props.buffer, key)).map(reading => ({
  key: reading.sensor.key,
  name: 'text' in reading.label ? reading.label.text : `${t(reading.label.key)}${reading.label.slot ? ` ${reading.label.slot}` : ''}`,
  text: `${formatNumber(reading.value, locale.value, digitsFor(reading.sensor.kind) + (reading.sensor.kind === 'Voltage' ? 1 : 0))} ${reading.sensor.unit}`,
})))
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="font-medium">
        {{ t('live.rails.title') }}
      </h3>
    </template>
    <div class="space-y-4">
      <LiveRailGauge
        v-for="rail in rails"
        :key="rail.key"
        :name="rail.name"
        :value="rail.value"
        :range="rail.range"
      />
      <template v-if="others.length > 0">
        <USeparator />
        <div class="grid grid-cols-2 gap-3 text-sm">
          <div
            v-for="other in others"
            :key="other.key"
          >
            <p class="text-muted">
              {{ other.name }}
            </p>
            <p class="font-medium tabular-nums">
              {{ other.text }}
            </p>
          </div>
        </div>
      </template>
    </div>
  </UCard>
</template>
