<script setup lang="ts">
// The night Caixa-Preta was born, as an 8-second loop: the cat sits on the PC case, leaps to the wardrobe, the push
// sends the case off the desk, and its power light blinks while it keeps restarting. Original line art in the theme's
// colours, animated with CSS only. With prefers-reduced-motion it shows the cat sitting on the case.
const { t } = useI18n()
</script>

<template>
  <svg
    viewBox="0 -50 480 380"
    role="img"
    :aria-label="t('story.illustration')"
    class="cat-fall h-auto w-full"
  >
    <!-- Room -->
    <line
      class="floor"
      x1="0"
      y1="300"
      x2="480"
      y2="300"
    />
    <g class="furniture">
      <!-- Desk (80 cm) -->
      <rect
        x="90"
        y="190"
        width="200"
        height="10"
      />
      <line
        x1="100"
        y1="200"
        x2="100"
        y2="300"
      />
      <line
        x1="280"
        y1="200"
        x2="280"
        y2="300"
      />
      <!-- Wardrobe -->
      <rect
        x="330"
        y="60"
        width="120"
        height="240"
      />
      <line
        x1="390"
        y1="60"
        x2="390"
        y2="300"
      />
      <line
        x1="380"
        y1="170"
        x2="380"
        y2="190"
      />
      <line
        x1="400"
        y1="170"
        x2="400"
        y2="190"
      />
    </g>
    <text
      class="height"
      x="296"
      y="250"
    >80 cm</text>
    <line
      class="height-mark"
      x1="306"
      y1="196"
      x2="306"
      y2="236"
    />
    <line
      class="height-mark"
      x1="306"
      y1="258"
      x2="306"
      y2="298"
    />

    <!-- PC case: pivots on its bottom-left corner when it goes over the edge -->
    <g class="case">
      <rect
        class="case-body"
        x="100"
        y="110"
        width="60"
        height="80"
      />
      <line
        class="case-detail"
        x1="110"
        y1="124"
        x2="150"
        y2="124"
      />
      <line
        class="case-detail"
        x1="110"
        y1="132"
        x2="150"
        y2="132"
      />
      <circle
        class="led"
        cx="130"
        cy="174"
        r="4"
      />
    </g>
    <g class="impact">
      <line
        x1="16"
        y1="292"
        x2="4"
        y2="282"
      />
      <line
        x1="96"
        y1="292"
        x2="108"
        y2="282"
      />
      <line
        x1="56"
        y1="232"
        x2="56"
        y2="218"
      />
    </g>

    <!-- The cat: one pose sitting, one leaping; the group carries it along the jump -->
    <g class="cat">
      <g class="pose sitting">
        <path d="M-16,0 C-20,-14 -16,-34 0,-36 C14,-36 18,-18 14,0 Z" />
        <circle
          cx="6"
          cy="-44"
          r="11"
        />
        <path d="M-1,-51 L1,-64 L8,-54 Z" />
        <path d="M8,-54 L15,-63 L16,-50 Z" />
        <path
          class="tail"
          d="M-14,-2 C-30,-2 -34,-18 -26,-28"
        />
        <circle
          class="eye"
          cx="11"
          cy="-46"
          r="1.6"
        />
      </g>
      <g class="pose leaping">
        <path d="M-26,-14 C-22,-26 6,-30 18,-22 C26,-16 22,-8 10,-6 C-6,-4 -22,-4 -26,-14 Z" />
        <circle
          cx="26"
          cy="-28"
          r="10"
        />
        <path d="M20,-35 L21,-47 L28,-37 Z" />
        <path d="M28,-37 L35,-45 L35,-32 Z" />
        <path
          class="leg"
          d="M10,-8 L26,-2"
        />
        <path
          class="leg"
          d="M-18,-8 L-34,0"
        />
        <path
          class="tail"
          d="M-26,-14 C-40,-16 -48,-24 -50,-34"
        />
      </g>
    </g>
  </svg>
</template>

<style scoped>
.cat-fall {
  --ink: var(--ui-text-muted);
  --cat: var(--ui-text-highlighted);
}

.floor,
.furniture rect,
.furniture line,
.height-mark {
  fill: none;
  stroke: var(--ink);
  stroke-width: 2;
  stroke-linecap: round;
}

.height {
  fill: var(--ui-text-muted);
  font-size: 12px;
}

.case-body {
  fill: var(--ui-bg-elevated);
  stroke: var(--ui-primary);
  stroke-width: 3;
}

.case-detail {
  stroke: var(--ink);
  stroke-width: 2;
}

.led {
  fill: var(--ui-primary);
}

.impact line {
  stroke: var(--ui-error);
  stroke-width: 2;
  stroke-linecap: round;
  opacity: 0;
}

.cat path,
.cat circle {
  fill: var(--cat);
}

.cat .tail,
.cat .leg {
  fill: none;
  stroke: var(--cat);
  stroke-width: 5;
  stroke-linecap: round;
}

.cat .eye {
  fill: var(--ui-bg);
}

/* Resting state (and the only one with reduced motion): the cat sits on the case. */
.cat {
  transform: translate(130px, 110px) scale(1.35);
}

.leaping {
  opacity: 0;
}

.case {
  transform-box: view-box;
  transform-origin: 100px 190px;
}

@media (prefers-reduced-motion: no-preference) {
  .cat {
    animation: cat-path 8s infinite;
  }

  .sitting {
    animation: sitting 8s infinite;
  }

  .leaping {
    animation: leaping 8s infinite;
  }

  .case {
    animation: case-fall 8s infinite;
  }

  .impact line {
    animation: impact 8s infinite;
  }

  .led {
    animation: led 8s infinite;
  }
}

/* 0–22 %: sitting on the case. 22–36 %: the leap, in an arc, to the top of the wardrobe. 36–92 %: on the wardrobe. */
@keyframes cat-path {
  0%,
  20% {
    transform: translate(130px, 110px) scale(1.35);
    opacity: 1;
  }
  22% {
    transform: translate(130px, 112px) scale(1.4, 1.2);
  }
  26% {
    transform: translate(195px, 66px) scale(1.35);
  }
  30% {
    transform: translate(265px, 42px) scale(1.35);
  }
  34% {
    transform: translate(340px, 52px) scale(1.35);
  }
  36%,
  92% {
    transform: translate(372px, 60px) scale(1.35);
    opacity: 1;
  }
  96%,
  100% {
    transform: translate(372px, 60px) scale(1.35);
    opacity: 0;
  }
}

@keyframes sitting {
  0%,
  22% {
    opacity: 1;
  }
  23%,
  35% {
    opacity: 0;
  }
  36%,
  100% {
    opacity: 1;
  }
}

@keyframes leaping {
  0%,
  22% {
    opacity: 0;
  }
  23%,
  35% {
    opacity: 1;
  }
  36%,
  100% {
    opacity: 0;
  }
}

/* The push of the leap slides the case off the left edge; it falls 80 cm and lands on its side. */
@keyframes case-fall {
  0%,
  24% {
    transform: none;
    opacity: 1;
  }
  30% {
    transform: translate(-6px, 0) rotate(-14deg);
  }
  36% {
    transform: translate(-4px, 40px) rotate(-55deg);
    animation-timing-function: ease-in;
  }
  40% {
    transform: translate(0, 110px) rotate(-90deg);
  }
  42% {
    transform: translate(0, 105px) rotate(-87deg);
  }
  44%,
  92% {
    transform: translate(0, 110px) rotate(-90deg);
    opacity: 1;
  }
  96% {
    transform: translate(0, 110px) rotate(-90deg);
    opacity: 0;
  }
  100% {
    transform: none;
    opacity: 0;
  }
}

@keyframes impact {
  0%,
  39% {
    opacity: 0;
  }
  40%,
  46% {
    opacity: 1;
  }
  50%,
  100% {
    opacity: 0;
  }
}

/* After the fall the power light keeps going off and on: the PC restarting by itself. */
@keyframes led {
  0%,
  40% {
    fill: var(--ui-primary);
  }
  48%,
  56%,
  66%,
  76%,
  86% {
    fill: var(--ui-error);
  }
  52%,
  61%,
  71%,
  81%,
  90% {
    fill: var(--ui-bg-accented);
  }
  100% {
    fill: var(--ui-primary);
  }
}
</style>
