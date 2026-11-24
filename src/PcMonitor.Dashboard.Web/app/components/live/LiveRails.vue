<script setup lang="ts">
import type { LiveBuffer } from '~/utils/live-buffer'

const props = defineProps<{ buffer: LiveBuffer }>()

const { t, locale } = useI18n()

const railOrder: readonly string[] = ['Rail12V', 'Rail5V', 'Rail3V3']

const rails = computed(() => props.buffer.sensors
  .filter(s => s.range !== null)
  .sort((a, b) => railOrder.indexOf(a.role) - railOrder.indexOf(b.role))
  .map(s => ({ key: s.key, name: t(`roles.${s.role}`), value: latestValue(props.buffer, s.key), range: s.range! })))

/** Vcore, fans and memory temperatures, by the names they were recorded with. */
const others = computed(() => props.buffer.sensors
  .filter(s => s.role === 'Vcore' || (s.role === 'None' && (s.kind === 'Fan' || s.kind === 'Temperature')))
  .map((s) => {
    const dimm = /^\/memory\/dimm\/(\d+)\//.exec(s.key)
    const name = s.role === 'Vcore' ? t('live.others.vcore') : dimm ? `${t('live.others.memory')} ${Number(dimm[1]) + 1}` : s.name
    const digits = s.kind === 'Fan' ? 0 : s.kind === 'Voltage' ? 3 : 1
    return { key: s.key, name, text: `${formatNumber(latestValue(props.buffer, s.key), locale.value, digits)} ${s.unit}` }
  }))
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
