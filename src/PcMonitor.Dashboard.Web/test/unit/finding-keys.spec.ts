import { describe, expect, it } from 'vitest'
import { findingKeys } from '~/utils/finding-keys'
import { loadLocale, localeKeys } from '~~/test/support/locale-keys'

describe('findingKeys', () => {
  it('has a message for every finding code in both locales', () => {
    for (const code of ['pt-BR', 'en'] as const) {
      const keys = localeKeys(loadLocale(code))
      for (const key of Object.values(findingKeys)) {
        expect(keys).toContain(key)
      }
    }
  })

  it('maps codes to keys without dots inside the code', () => {
    expect(findingKeys['bugcheck.none']).toBe('findings.bugcheck_none')
    expect(findingKeys.bugcheck).toBe('findings.bugcheck')
  })
})
