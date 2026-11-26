import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { SCREENS, screenSrc } from '~/utils/screens'

const file = (path: string) => fileURLToPath(new URL(path, import.meta.url))

/** Width and height from a PNG header (IHDR). */
function pngSize(path: string): [number, number] {
  const png = readFileSync(path)
  return [png.readUInt32BE(16), png.readUInt32BE(20)]
}

/** Width and height from a WebP header (VP8, VP8L or VP8X). */
function webpSize(path: string): [number, number] {
  const webp = readFileSync(path)
  expect(webp.toString('ascii', 0, 4) + webp.toString('ascii', 8, 12)).toBe('RIFFWEBP')
  const chunk = webp.toString('ascii', 12, 16)
  if (chunk === 'VP8X') {
    return [1 + webp.readUIntLE(24, 3), 1 + webp.readUIntLE(27, 3)]
  }
  if (chunk === 'VP8L') {
    const bits = webp.readUInt32LE(21)
    return [1 + (bits & 0x3FFF), 1 + ((bits >> 14) & 0x3FFF)]
  }
  return [webp.readUInt16LE(26) & 0x3FFF, webp.readUInt16LE(28) & 0x3FFF]
}

describe('screenshots', () => {
  it.each(SCREENS.map(screen => [screen.id, screen] as const))('%s exists at both widths with the declared size', (_id, screen) => {
    expect(screen.widths[0]).toBe(screen.width)
    for (const width of screen.widths) {
      const [w, h] = webpSize(file(`../../public${screenSrc(screen.id, width)}`))
      expect(w).toBe(width)
      expect(h).toBe(Math.round(width * screen.height / screen.width))
    }
  })

  it('marks the demo-database screenshot as sample data, and only that one', () => {
    expect(SCREENS.filter(screen => screen.sample).map(screen => screen.id)).toEqual(['incident'])
  })
})

describe('Open Graph images', () => {
  it.each(['pt-BR', 'en'])('og-%s.png is 1200 × 630', (code) => {
    expect(pngSize(file(`../../public/og/og-${code}.png`))).toEqual([1200, 630])
  })
})

describe('icons', () => {
  it('ship the single brand source unchanged', () => {
    expect(readFileSync(file('../../public/icon.svg'), 'utf8')).toBe(readFileSync(file('../../../../assets/brand/caixa-preta.svg'), 'utf8'))
  })

  it('ship a favicon', () => {
    const ico = readFileSync(file('../../public/favicon.ico'))
    expect([ico.readUInt16LE(0), ico.readUInt16LE(2)]).toEqual([0, 1])
  })
})
