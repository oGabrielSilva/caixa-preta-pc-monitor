// Generates every icon file from the single brand source (assets/brand/caixa-preta.svg, ADR 0017).
// Run with `corepack pnpm icons` and commit the results.
import { copyFileSync, readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { Resvg } from '@resvg/resvg-js'

const path = relative => fileURLToPath(new URL(relative, import.meta.url))
const brand = path('../../../assets/brand/')
const svg = readFileSync(`${brand}caixa-preta.svg`)

const png = size => new Resvg(svg, { fitTo: { mode: 'width', value: size } }).render().asPng()

/** An ICO file whose images are PNGs (supported since Windows Vista). */
function ico(sizes) {
  const images = sizes.map(png)
  const header = Buffer.alloc(6 + 16 * images.length)
  header.writeUInt16LE(0, 0)
  header.writeUInt16LE(1, 2)
  header.writeUInt16LE(images.length, 4)
  let offset = header.length
  images.forEach((image, i) => {
    const entry = 6 + 16 * i
    header.writeUInt8(sizes[i] >= 256 ? 0 : sizes[i], entry)
    header.writeUInt8(sizes[i] >= 256 ? 0 : sizes[i], entry + 1)
    header.writeUInt16LE(1, entry + 4)
    header.writeUInt16LE(32, entry + 6)
    header.writeUInt32LE(image.length, entry + 8)
    header.writeUInt32LE(offset, entry + 12)
    offset += image.length
  })
  return Buffer.concat([header, ...images])
}

writeFileSync(`${brand}caixa-preta.ico`, ico([16, 20, 24, 32, 40, 48, 64, 256]))
writeFileSync(`${brand}caixa-preta-256.png`, png(256))
// The dashboard and the landing page (PcMonitor.Site) ship the same favicon and SVG icon.
for (const app of ['../public/', '../../PcMonitor.Site/public/']) {
  writeFileSync(path(`${app}favicon.ico`), ico([16, 32, 48]))
  copyFileSync(`${brand}caixa-preta.svg`, path(`${app}icon.svg`))
}
console.log('Icons generated from assets/brand/caixa-preta.svg')
