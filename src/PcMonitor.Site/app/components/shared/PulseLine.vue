<script setup lang="ts">
// The recorder's trace: steady, one heartbeat (the brand icon's pulse), steady again, then the drop to the floor
// where the PC went dark. It draws itself when it comes into view, then a pulse of light keeps running along it and
// the crash point beats, like a recorder that is still running. Nothing moves with prefers-reduced-motion.
const { t } = useI18n()

const trace = 'M0,60 L380,60 L404,60 L420,30 L440,96 L462,12 L480,74 L492,60 L1120,60 C1180,60 1210,70 1240,96 L1268,112 L1600,112'

const root = ref<SVGSVGElement>()
const state = ref<'idle' | 'waiting' | 'drawing'>('idle')

onMounted(() => {
  if (!root.value || window.matchMedia('(prefers-reduced-motion: reduce)').matches || !('IntersectionObserver' in window)) {
    return
  }
  state.value = 'waiting'
  const observer = new IntersectionObserver((entries) => {
    if (entries.some(entry => entry.isIntersecting)) {
      state.value = 'drawing'
      observer.disconnect()
    }
  }, { threshold: 0.4 })
  observer.observe(root.value)
})
</script>

<template>
  <svg
    ref="root"
    viewBox="0 0 1600 128"
    preserveAspectRatio="none"
    role="img"
    :aria-label="t('hero.traceLabel')"
    class="pulse-line h-20 w-full overflow-visible sm:h-28"
    :class="state"
  >
    <path
      :d="trace"
      pathLength="1"
      fill="none"
      class="trace stroke-primary"
      stroke-width="3"
      vector-effect="non-scaling-stroke"
      stroke-linejoin="round"
      stroke-linecap="round"
    />
    <path
      :d="trace"
      pathLength="1"
      fill="none"
      class="signal stroke-primary"
      stroke-width="5"
      vector-effect="non-scaling-stroke"
      stroke-linejoin="round"
      stroke-linecap="round"
    />
    <circle
      cx="1268"
      cy="112"
      r="7"
      class="ring fill-none stroke-error"
      stroke-width="2"
      vector-effect="non-scaling-stroke"
    />
    <circle
      cx="1268"
      cy="112"
      r="7"
      class="dot fill-error"
    />
  </svg>
</template>

<style scoped>
.trace {
  stroke-dasharray: 1;
  stroke-dashoffset: 0;
}

.signal,
.ring {
  opacity: 0;
}

.dot,
.ring {
  transform-box: fill-box;
  transform-origin: center;
}

/* Hidden until it scrolls into view (only when JavaScript and motion are available). */
.waiting .trace {
  stroke-dashoffset: 1;
}

.waiting .dot {
  opacity: 0;
}

.drawing .trace {
  animation: trace 2.2s cubic-bezier(0.5, 0, 0.2, 1) both;
}

.drawing .dot {
  animation: land 0.4s ease-out 2.1s both;
}

/* A short bright segment that runs along the trace and fades out where the PC went dark. */
.drawing .signal {
  stroke-dasharray: 0.05 1;
  filter: drop-shadow(0 0 6px var(--ui-primary));
  animation: signal 3.6s linear 2.4s infinite;
}

.drawing .ring {
  animation: ring 1.8s ease-out 2.5s infinite;
}

@keyframes trace {
  from {
    stroke-dashoffset: 1;
  }
}

@keyframes land {
  from {
    opacity: 0;
    transform: scale(0.2);
  }
}

@keyframes signal {
  0% {
    stroke-dashoffset: 0.05;
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    stroke-dashoffset: -0.8;
    opacity: 0;
  }
}

@keyframes ring {
  0% {
    opacity: 0.9;
    transform: scale(1);
  }
  100% {
    opacity: 0;
    transform: scale(3.2);
  }
}
</style>
