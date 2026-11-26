<script setup lang="ts">
import type { ExportFormat } from '~/composables/useExport'

const props = defineProps<{ formats: readonly ExportFormat[], busy?: boolean }>()
const emit = defineEmits<{ select: [format: ExportFormat] }>()

const { t } = useI18n()

const icons: Record<ExportFormat, string> = {
  csv: 'i-material-symbols-csv-outline-rounded',
  xlsx: 'i-material-symbols-table-outline-rounded',
  pdf: 'i-material-symbols-picture-as-pdf-outline-rounded',
}

const items = computed(() => props.formats.map(format => ({
  label: t(`export.formats.${format}`),
  icon: icons[format],
  onSelect: () => emit('select', format),
})))
</script>

<template>
  <UDropdownMenu :items="items">
    <UButton
      color="neutral"
      variant="outline"
      size="sm"
      icon="i-material-symbols-download-rounded"
      :loading="props.busy"
      :label="t('export.button')"
    />
  </UDropdownMenu>
</template>
