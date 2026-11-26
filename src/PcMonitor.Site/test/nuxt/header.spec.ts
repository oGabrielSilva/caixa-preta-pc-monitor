// @vitest-environment nuxt
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import SiteHeader from '~/components/shared/SiteHeader.vue'

describe('SiteHeader', () => {
  it('chooses the language with a select labelled for screen readers', async () => {
    const header = await mountSuspended(SiteHeader)
    const select = header.find('[data-language-select]')
    expect(select.attributes('role')).toBe('combobox')
    expect(select.attributes('aria-label')).toBeTruthy()
    expect(header.text()).toContain('Português')
  })
})
