<script setup lang="ts">
// The always-on-top panel window of the tray app: no navigation, transparent around the panel.
useHead({ htmlAttrs: { class: 'mini-window' } })
</script>

<template>
  <div class="h-svh w-full">
    <slot />
  </div>
</template>
