<template>
  <div class="flex min-h-svh flex-col">
    <SiteHeader />
    <main
      id="conteudo"
      class="flex-1"
    >
      <slot />
    </main>
    <SiteFooter />
  </div>
</template>
