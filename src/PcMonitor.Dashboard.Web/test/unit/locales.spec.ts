import { describe, expect, it } from 'vitest'
import { dottedKeys, localeValues } from '../../../PcMonitor.Brand/shared/locale-keys'
import { loadLocale, localeKeys } from '~~/test/support/locale-keys'

describe('locales', () => {
  const ptBR = localeKeys(loadLocale('pt-BR'))
  const en = localeKeys(loadLocale('en'))

  it('pt-BR is not empty', () => {
    expect(ptBR.length).toBeGreaterThan(0)
  })

  it('name the product Caixa-Preta, never the internal name (ADR 0017)', () => {
    expect([...localeValues(loadLocale('pt-BR')), ...localeValues(loadLocale('en'))].filter(text => text.includes('PC Monitor'))).toEqual([])
  })

  it('pt-BR and en have the same keys', () => {
    expect([...en].sort()).toEqual([...ptBR].sort())
  })
})

describe('locale keys', () => {
  it('never contain a dot inside a key (vue-i18n reads dots as nesting)', () => {
    expect(dottedKeys(loadLocale('pt-BR'))).toEqual([])
  })
})
