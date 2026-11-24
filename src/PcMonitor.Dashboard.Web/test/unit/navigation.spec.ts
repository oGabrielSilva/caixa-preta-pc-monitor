import { describe, expect, it } from 'vitest'
import { navigationItems } from '~/utils/navigation'
import { loadLocale, localeKeys } from '~~/test/support/locale-keys'

describe('navigationItems', () => {
  it('links the live, incidents and history pages in that order', () => {
    expect(navigationItems.map(item => item.to)).toEqual(['/', '/incidents', '/history'])
  })

  it('has a pt-BR translation for every label', () => {
    const keys = localeKeys(loadLocale('pt-BR'))
    for (const item of navigationItems) {
      expect(keys).toContain(item.labelKey)
    }
  })
})
