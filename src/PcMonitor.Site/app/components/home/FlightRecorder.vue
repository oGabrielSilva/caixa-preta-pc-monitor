<script setup lang="ts">
// An illustrative crash, drawn from fixed numbers that mirror the demo database (screenshots/DemoDatabase.cs):
// +12 V steady at about 12.10 V, then sagging for 40 s below the ATX minimum, then the PC is off.
const { t } = useI18n()

const ATX = { min: 11.4, max: 12.6 }
const Y = { top: 12.8, bottom: 11.0 }
const width = 1000
const height = 260
const recorded = 720

const y = (volts: number) => ((Y.top - volts) / (Y.top - Y.bottom)) * height
const samples = Array.from({ length: 81 }, (_, i) => {
  const toEnd = 80 - i
  const jitter = 0.012 * Math.sin(i * 1.7) + 0.008 * Math.sin(i * 0.43)
  return toEnd > 40 ? 12.1 + jitter : 12.1 - (40 - toEnd) * 0.022 + jitter / 2
})
const points = samples.map((volts, i) => `${((i / 80) * recorded).toFixed(1)},${y(volts).toFixed(1)}`).join(' ')
const last = { x: recorded, y: y(samples[80]!) }

const steps = ['recording', 'crash', 'restart', 'diagnosis'] as const
const evidence = [
  { key: 'for', icon: 'i-material-symbols-add-rounded', tone: 'text-primary' },
  { key: 'for2', icon: 'i-material-symbols-add-rounded', tone: 'text-primary' },
  { key: 'against', icon: 'i-material-symbols-remove-rounded', tone: 'text-muted' },
  { key: 'against2', icon: 'i-material-symbols-remove-rounded', tone: 'text-muted' },
] as const
</script>

<template>
  <section
    id="como-funciona"
    class="scroll-mt-20 border-t border-default"
  >
    <div class="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:py-28">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div class="max-w-2xl">
          <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
            {{ t('recorder.title') }}
          </h2>
          <p class="mt-4 text-lg text-toned">
            {{ t('recorder.lead') }}
          </p>
        </div>
        <UBadge
          variant="outline"
          color="neutral"
          icon="i-material-symbols-info-outline-rounded"
          :label="t('recorder.sample')"
        />
      </div>

      <figure class="mt-12 border border-default bg-elevated/40 p-4 sm:p-6">
        <svg
          :viewBox="`0 0 ${width} ${height}`"
          role="img"
          :aria-label="t('recorder.chartLabel')"
          class="h-auto w-full"
        >
          <rect
            x="0"
            :y="y(ATX.max)"
            :width="recorded"
            :height="y(ATX.min) - y(ATX.max)"
            class="fill-primary/8"
          />
          <line
            x1="0"
            :x2="recorded"
            :y1="y(ATX.min)"
            :y2="y(ATX.min)"
            class="stroke-primary/50"
            stroke-dasharray="4 6"
          />
          <text
            x="8"
            :y="y(ATX.max) + 20"
            fill="var(--ui-text-muted)"
            font-size="14"
          >{{ t('recorder.band') }}</text>
          <polyline
            :points="points"
            fill="none"
            class="stroke-primary"
            stroke-width="3"
            stroke-linejoin="round"
          />
          <circle
            :cx="last.x"
            :cy="last.y"
            r="7"
            class="fill-error"
          />
          <rect
            :x="recorded + 12"
            y="0"
            :width="width - recorded - 12"
            :height="height"
            fill="url(#off)"
          />
          <defs>
            <pattern
              id="off"
              width="10"
              height="10"
              patternUnits="userSpaceOnUse"
              patternTransform="rotate(45)"
            >
              <line
                x1="0"
                y1="0"
                x2="0"
                y2="10"
                stroke="var(--ui-border-accented)"
                stroke-width="2"
              />
            </pattern>
          </defs>
          <text
            :x="(recorded + width) / 2 + 6"
            :y="height / 2"
            text-anchor="middle"
            fill="var(--ui-text-toned)"
            font-size="16"
            font-weight="700"
          >{{ t('recorder.off') }}</text>
          <g
            fill="var(--ui-text-muted)"
            font-size="13"
          >
            <text
              x="0"
              :y="height - 4"
            >−40 s</text>
            <text
              :x="recorded"
              :y="height - 4"
              text-anchor="end"
            >0 s</text>
            <text
              :x="width"
              :y="height - 4"
              text-anchor="end"
            >+2 min</text>
          </g>
        </svg>
      </figure>

      <div class="mt-12 grid gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
        <ol class="relative flex flex-col gap-8 border-l border-accented pl-8">
          <li
            v-for="(step, index) in steps"
            :key="step"
            class="relative"
          >
            <span
              class="absolute -left-[2.85rem] flex size-7 items-center justify-center border text-sm font-bold tabular-nums"
              :class="step === 'crash' ? 'border-error bg-error text-inverted' : 'border-primary bg-default text-primary'"
              aria-hidden="true"
            >{{ index + 1 }}</span>
            <h3 class="text-lg font-bold text-highlighted">
              {{ t(`recorder.steps.${step}.title`) }}
            </h3>
            <p class="mt-1 text-toned">
              {{ t(`recorder.steps.${step}.text`) }}
            </p>
          </li>
        </ol>

        <div class="border border-primary/40 bg-elevated p-6 sm:p-8">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <p class="text-toned">
              {{ t('recorder.verdict.label') }}
            </p>
            <UBadge
              color="primary"
              variant="outline"
              :label="t('recorder.verdict.confidence')"
            />
          </div>
          <p class="mt-2 text-5xl font-extrabold tracking-tight text-highlighted">
            {{ t('recorder.verdict.cause') }}
          </p>
          <ul class="mt-6 flex flex-col gap-3">
            <li
              v-for="item in evidence"
              :key="item.key"
              class="flex gap-3"
            >
              <UIcon
                :name="item.icon"
                class="mt-0.5 size-5 shrink-0"
                :class="item.tone"
              />
              <span class="text-default">{{ t(`recorder.verdict.${item.key}`) }}</span>
            </li>
          </ul>
          <p class="mt-6 flex gap-3 border-t border-default pt-4 text-sm text-muted">
            <UIcon
              name="i-material-symbols-info-outline-rounded"
              class="mt-0.5 size-4 shrink-0 text-warning"
            />
            {{ t('recorder.verdict.limit') }}
          </p>
        </div>
      </div>
    </div>
  </section>
</template>
