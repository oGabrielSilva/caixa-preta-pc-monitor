import { describe, expect, it } from 'vitest'
import { loadLocale, localeKeys } from '~~/test/support/locale-keys'

describe('locales', () => {
  const ptBR = localeKeys(loadLocale('pt-BR'))
  const en = localeKeys(loadLocale('en'))

  it('pt-BR is not empty', () => {
    expect(ptBR.length).toBeGreaterThan(0)
  })

  it('pt-BR and en have the same keys', () => {
    expect([...en].sort()).toEqual([...ptBR].sort())
  })
})

describe('locale keys', () => {
  it('never contain a dot inside a key (vue-i18n reads dots as nesting)', () => {
    const leaves = (messages: object): string[] => Object.entries(messages).flatMap(([key, value]) =>
      typeof value === 'string' ? [key] : [key, ...leaves(value as object)])
    expect(leaves(loadLocale('pt-BR')).filter(key => key.includes('.'))).toEqual([])
  })
})
