<script setup lang="ts">
// The recorder's trace: steady, one heartbeat (the brand icon's pulse), steady again, then the drop to the floor
// where the PC went dark. Built from pieces so the heartbeat and the drop keep their shape at any width (only the
// flat stretches grow). It draws itself when it comes into view; then the heartbeat keeps beating and the crash point
// keeps pulsing, like a recorder that is still running. Nothing moves with prefers-reduced-motion.
const { t } = useI18n()

const root = ref<HTMLElement>()
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
  <div
    ref="root"
    role="img"
    :aria-label="t('hero.traceLabel')"
    class="pulse-line flex h-24 w-full items-stretch"
    :class="state"
  >
    <!-- Flat stretches: a horizontal line can stretch sideways without changing its thickness. -->
    <svg
      class="piece flat min-w-0 basis-0 grow-[3]"
      viewBox="0 0 100 96"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <path
        d="M0,40 L100,40"
        pathLength="1"
      />
    </svg>
    <svg
      class="piece beat w-28 shrink-0"
      viewBox="0 0 112 96"
      aria-hidden="true"
    >
      <path
        d="M0,40 L24,40 L40,16 L58,76 L78,6 L94,56 L102,40 L112,40"
        pathLength="1"
      />
    </svg>
    <svg
      class="piece flat min-w-0 basis-0 grow-[6]"
      viewBox="0 0 100 96"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <path
        d="M0,40 L100,40"
        pathLength="1"
      />
    </svg>
    <svg
      class="piece drop w-40 shrink-0"
      viewBox="0 0 160 96"
      aria-hidden="true"
    >
      <path
        d="M0,40 L36,40 C76,40 96,50 116,72 L130,84 L160,84"
        pathLength="1"
      />
      <circle
        class="ring"
        cx="130"
        cy="84"
        r="6"
      />
      <circle
        class="dot"
        cx="130"
        cy="84"
        r="6"
      />
    </svg>
    <svg
      class="piece floor min-w-0 basis-0 grow-[2]"
      viewBox="0 0 100 96"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <path
        d="M0,84 L100,84"
        pathLength="1"
      />
    </svg>
  </div>
</template>

<style scoped>
.piece {
  height: 100%;
  overflow: visible;
}

.piece path {
  fill: none;
  stroke: var(--ui-primary);
  stroke-width: 3px;
  stroke-linejoin: round;
  stroke-linecap: round;
  stroke-dasharray: 1;
  stroke-dashoffset: 0;
}

.dot {
  fill: var(--ui-error);
}

.ring {
  fill: none;
  stroke: var(--ui-error);
  stroke-width: 2px;
  opacity: 0;
}

.dot,
.ring,
.beat path {
  transform-box: fill-box;
  transform-origin: center;
}

/* Hidden until it scrolls into view (only when JavaScript and motion are available). */
.waiting .piece path {
  stroke-dashoffset: 1;
}

.waiting .dot {
  opacity: 0;
}

/* The pieces draw one after the other, left to right, at about the same speed. */
.drawing .piece path {
  animation: draw 0.5s linear both;
}
.drawing .flat:nth-child(1) path { animation-duration: 0.45s; }
.drawing .beat path { animation-delay: 0.45s; animation-duration: 0.35s; }
.drawing .flat:nth-child(3) path { animation-delay: 0.8s; animation-duration: 0.7s; }
.drawing .drop path { animation-delay: 1.5s; animation-duration: 0.45s; }
.drawing .floor path { animation-delay: 1.95s; animation-duration: 0.3s; }

.drawing .dot {
  animation: land 0.3s ease-out 1.9s both;
}

/* After the first draw: the heartbeat flashes every few seconds and the crash point keeps pulsing. */
.drawing .beat path {
  animation:
    draw 0.35s linear 0.45s both,
    beat 3s ease-in-out 2.4s infinite;
}

.drawing .ring {
  animation: ring 1.8s ease-out 2.3s infinite;
}

@keyframes draw {
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

@keyframes beat {
  0%,
  70%,
  100% {
    filter: none;
  }
  80% {
    filter: drop-shadow(0 0 6px var(--ui-primary));
  }
}

@keyframes ring {
  0% {
    opacity: 0.9;
    transform: scale(1);
  }
  100% {
    opacity: 0;
    transform: scale(3);
  }
}
</style>
