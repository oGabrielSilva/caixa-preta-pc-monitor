import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { loadLocale } from '~~/test/support/locale-keys'

const file = (path: string) => fileURLToPath(new URL(path, import.meta.url))

describe('brand', () => {
  it('names the app Caixa-Preta in both languages', () => {
    for (const code of ['pt-BR', 'en'] as const) {
      expect((loadLocale(code).app as Record<string, string>).title).toBe('Caixa-Preta')
    }
  })

  it('ships a favicon generated from the brand icon', () => {
    const ico = readFileSync(file('../../public/favicon.ico'))
    // ICONDIR: reserved 0, type 1 (icon), at least one image.
    expect([ico.readUInt16LE(0), ico.readUInt16LE(2)]).toEqual([0, 1])
    expect(ico.readUInt16LE(4)).toBeGreaterThan(0)
  })

  it('keeps the shipped SVG icon identical to the single brand source', () => {
    expect(readFileSync(file('../../public/icon.svg'), 'utf8')).toBe(readFileSync(file('../../../../assets/brand/caixa-preta.svg'), 'utf8'))
  })
})
