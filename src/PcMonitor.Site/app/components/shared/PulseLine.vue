<script setup lang="ts">
// The recorder's trace: steady, one heartbeat (the brand icon's pulse), steady again, then the drop to the floor
// where the PC went dark. It draws itself once when the page loads (the page's only ambient motion).
const { t } = useI18n()

const trace = 'M0,60 L380,60 L404,60 L420,30 L440,96 L462,12 L480,74 L492,60 L1120,60 C1180,60 1210,70 1240,96 L1268,112 L1600,112'
</script>

<template>
  <svg
    viewBox="0 0 1600 128"
    preserveAspectRatio="none"
    role="img"
    :aria-label="t('hero.traceLabel')"
    class="pulse-line h-20 w-full sm:h-28"
  >
    <path
      :d="trace"
      pathLength="1"
      fill="none"
      class="stroke-primary"
      stroke-width="3"
      vector-effect="non-scaling-stroke"
      stroke-linejoin="round"
      stroke-linecap="round"
    />
    <circle
      cx="1268"
      cy="112"
      r="7"
      class="fill-error pulse-line-dot"
    />
  </svg>
</template>

<style scoped>
.pulse-line path {
  stroke-dasharray: 1;
  stroke-dashoffset: 0;
}

.pulse-line-dot {
  transform-box: fill-box;
  transform-origin: center;
}

@media (prefers-reduced-motion: no-preference) {
  .pulse-line path {
    animation: trace 2.4s cubic-bezier(0.5, 0, 0.2, 1) both;
  }

  .pulse-line-dot {
    animation: land 0.4s ease-out 2.3s both;
  }
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
</style>
