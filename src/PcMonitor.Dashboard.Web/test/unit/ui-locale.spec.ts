import { describe, expect, it } from 'vitest'
import { appLocales } from '~~/i18n/app-locales'
import { uiLocaleFor } from '~/utils/ui-locale'

describe('uiLocaleFor', () => {
  it.each(appLocales.map(l => l.code))('gives Nuxt UI the %s translation of its own texts', (code) => {
    expect(uiLocaleFor(code).code).toBe(code)
  })

  it('falls back to pt-BR for an unknown locale', () => {
    expect(uiLocaleFor('fr').code).toBe('pt-BR')
  })
})
