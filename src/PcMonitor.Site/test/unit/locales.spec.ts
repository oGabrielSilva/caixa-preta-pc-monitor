import { describe, expect, it } from 'vitest'
import { dottedKeys, localeKeys, localeValues } from '../../../PcMonitor.Brand/shared/locale-keys'
import { siteMessages } from '../support/locales'

const languages = ['pt-BR', 'en'] as const

describe('site texts', () => {
  it('exist in English for every Portuguese key, and the other way round', () => {
    const [portuguese, english] = languages.map(code => localeKeys(siteMessages(code)).sort())
    expect(english).toEqual(portuguese)
  })

  it.each(languages)('%s has no key with a dot inside', (code) => {
    expect(dottedKeys(siteMessages(code))).toEqual([])
  })

  it.each(languages)('%s names the product Caixa-Preta and shows no e-mail address', (code) => {
    const texts = localeValues(siteMessages(code))
    expect(texts.filter(text => text.includes('PC Monitor') || /\S+@\S+\.\S+/.test(text))).toEqual([])
  })
})
