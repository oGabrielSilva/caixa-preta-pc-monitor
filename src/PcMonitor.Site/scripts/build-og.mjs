// Generates the Open Graph images (public/og/og-<locale>.png, 1200 × 630) from the brand icon (assets/brand,
// ADR 0017) and the slogan of each language. Text uses the system font (Segoe UI on Windows). Run with `pnpm og` and
// commit the results.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { Resvg } from '@resvg/resvg-js'

const path = relative => fileURLToPath(new URL(relative, import.meta.url))
const icon = readFileSync(path('../../../assets/brand/caixa-preta.svg')).toString('base64')

const slogans = {
  'pt-BR': ['Seu PC desligou sozinho?', 'A Caixa-Preta grava os sensores e explica a causa provável.', 'Windows 11 · gratuito · código aberto'],
  'en': ['Did your PC shut down on its own?', 'Caixa-Preta records the sensors and explains the likely cause.', 'Windows 11 · free · open source'],
}

const teal = '#2dd4bf'

/** A flat trace with one heartbeat, then a sag to the floor: the recording up to the crash. */
const trace = 'M0,520 L420,520 L452,470 L486,575 L520,430 L548,535 L566,520 L940,520 C990,520 1010,540 1040,566 L1080,600 L1200,600'

function og([title, subtitle, facts]) {
  const grid = Array.from({ length: 21 }, (_, i) => `<line x1="${i * 60}" y1="0" x2="${i * 60}" y2="630" />`).join('')
    + Array.from({ length: 11 }, (_, i) => `<line x1="0" y1="${i * 60}" x2="1200" y2="${i * 60}" />`).join('')
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
  <rect width="1200" height="630" fill="#0a0a0a"/>
  <g stroke="#ffffff" stroke-opacity="0.045" stroke-width="1">${grid}</g>
  <path d="${trace}" fill="none" stroke="${teal}" stroke-opacity="0.9" stroke-width="5" stroke-linejoin="round" stroke-linecap="round"/>
  <circle cx="1080" cy="600" r="9" fill="#fb7185"/>
  <image href="data:image/svg+xml;base64,${icon}" x="80" y="80" width="96" height="96"/>
  <text x="200" y="146" font-family="Segoe UI" font-weight="700" font-size="44" fill="#fafafa">Caixa-Preta</text>
  <text x="80" y="290" font-family="Segoe UI" font-weight="800" font-size="58" fill="#fafafa">${title}</text>
  <text x="80" y="350" font-family="Segoe UI" font-size="30" fill="#d4d4d4">${subtitle}</text>
  <text x="80" y="410" font-family="Segoe UI" font-weight="600" font-size="24" fill="${teal}">${facts}</text>
</svg>`
}

mkdirSync(path('../public/og'), { recursive: true })
for (const [locale, text] of Object.entries(slogans)) {
  const png = new Resvg(og(text), { font: { loadSystemFonts: true, defaultFontFamily: 'Segoe UI' } }).render().asPng()
  writeFileSync(path(`../public/og/og-${locale}.png`), png)
  console.log(`public/og/og-${locale}.png`)
}
