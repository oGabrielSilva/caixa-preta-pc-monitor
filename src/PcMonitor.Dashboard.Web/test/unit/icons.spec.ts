import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import icons from '@iconify-json/material-symbols/icons.json'

// The app and the Brand layer it extends (whose app.config.ts maps the Nuxt UI icons).
const appDirs = ['../../app', '../../../PcMonitor.Brand/app'].map(dir => fileURLToPath(new URL(dir, import.meta.url)))

function sources(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name)
    return statSync(path).isDirectory() ? sources(path) : /\.(vue|ts)$/.test(name) ? [path] : []
  })
}

const used = appDirs.flatMap(sources).flatMap(file => [...readFileSync(file, 'utf8').matchAll(/(?<![\w-])i-[a-z0-9]+(?:[-:][a-z0-9]+)+/g)].map(m => m[0]))

describe('icons', () => {
  it('come only from the bundled Material Symbols set (the theme chosen by the owner)', () => {
    expect(used.filter(name => !name.startsWith('i-material-symbols-') && !name.startsWith('i-material-symbols:'))).toEqual([])
  })

  it('all exist in the bundled set, so none renders blank offline', () => {
    const known = new Set([...Object.keys(icons.icons), ...Object.keys(icons.aliases ?? {})])
    expect([...new Set(used)].map(name => name.slice('i-material-symbols-'.length)).filter(name => !known.has(name))).toEqual([])
  })

  it('never start with a name Iconify reads as another collection', () => {
    // `i-material-symbols-light-mode` is icon `mode` of the `material-symbols-light` collection, which is not bundled:
    // such names must use the colon form (`i-material-symbols:light-mode-…`).
    expect(used.filter(name => name.startsWith('i-material-symbols-light-'))).toEqual([])
  })
})
