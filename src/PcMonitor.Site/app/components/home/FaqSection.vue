<script setup lang="ts">
import type { AccordionItem } from '@nuxt/ui'

const { t } = useI18n()

// The same list feeds the FAQPage JSON-LD (pages/index.vue). Answers stay in the HTML when closed, for crawlers.
const items = computed<AccordionItem[]>(() => FAQ_IDS.map(id => ({
  label: t(faqKeys(id).question),
  content: t(faqKeys(id).answer),
  value: id,
})))
</script>

<template>
  <section
    id="perguntas"
    class="scroll-mt-20 border-t border-default bg-muted/40"
  >
    <div class="mx-auto grid max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,2fr)] lg:py-28">
      <h2 class="text-3xl font-extrabold tracking-tight text-highlighted sm:text-5xl">
        {{ t('faq.title') }}
      </h2>
      <UAccordion
        :items="items"
        type="multiple"
        :unmount-on-hide="false"
        :default-value="['whyShutdown']"
        :ui="{ trigger: 'py-5 text-base', label: 'text-highlighted font-bold text-left', body: 'pb-5 text-base text-toned leading-relaxed max-w-[68ch]' }"
      >
        <template #default="{ item }">
          <h3
            data-faq-question
            class="text-base font-bold"
          >
            {{ item.label }}
          </h3>
        </template>
      </UAccordion>
    </div>
  </section>
</template>
