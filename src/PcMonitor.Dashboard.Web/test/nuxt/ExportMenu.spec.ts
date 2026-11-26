// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import ExportMenu from '~/components/shared/ExportMenu.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('ExportMenu', () => {
  it('offers the formats of the page under one button', async () => {
    const menu = await mountPtBR(ExportMenu, { props: { formats: ['csv', 'pdf'] } })

    expect(menu.text()).toContain('Exportar')
    expect(menu.html()).not.toContain('Excel')
  })
})
