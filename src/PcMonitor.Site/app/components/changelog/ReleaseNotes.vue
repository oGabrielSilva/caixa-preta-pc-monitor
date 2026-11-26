<script setup lang="ts">
import { inlineParts, noteBlocks } from '~/utils/release-notes'

// Text only, escaped by Vue (no v-html): release notes are written on GitHub, not by this site.
const props = defineProps<{ notes: string }>()
const blocks = computed(() => noteBlocks(props.notes))
</script>

<template>
  <div class="flex flex-col gap-3 text-toned">
    <template
      v-for="(block, index) in blocks"
      :key="index"
    >
      <h3
        v-if="block.kind === 'heading'"
        class="pt-2 font-bold text-highlighted"
      >
        {{ block.text }}
      </h3>
      <ul
        v-else-if="block.kind === 'list'"
        class="flex list-disc flex-col gap-1 pl-5 marker:text-primary"
      >
        <li
          v-for="(item, i) in block.items"
          :key="i"
        >
          <template
            v-for="(part, p) in inlineParts(item)"
            :key="p"
          >
            <a
              v-if="part.href"
              :href="part.href"
              class="break-all text-primary hover:underline"
            >{{ part.text }}</a>
            <template v-else>
              {{ part.text }}
            </template>
          </template>
        </li>
      </ul>
      <p v-else>
        <template
          v-for="(part, p) in inlineParts(block.text)"
          :key="p"
        >
          <a
            v-if="part.href"
            :href="part.href"
            class="break-all text-primary hover:underline"
          >{{ part.text }}</a>
          <template v-else>
            {{ part.text }}
          </template>
        </template>
      </p>
    </template>
  </div>
</template>
