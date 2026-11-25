// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import Page from '~/components/shared/Page.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('Page', () => {
  it('shows the title, the actions and the content', async () => {
    const page = await mountPtBR(Page, {
      props: { title: 'Incidentes' },
      slots: { right: () => 'ações', toolbar: () => 'filtros', default: () => 'conteúdo' },
    })

    expect(page.get('h1').text()).toBe('Incidentes')
    expect(page.text()).toContain('ações')
    expect(page.text()).toContain('filtros')
    expect(page.text()).toContain('conteúdo')
    expect(page.find('button[aria-label="Mostrar ou recolher o menu"]').exists()).toBe(true)
  })
})
