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
