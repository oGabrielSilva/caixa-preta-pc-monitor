import { readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const root = fileURLToPath(new URL('../..', import.meta.url))
// Build output may contain en/releases (the English page); only source folders matter.
const skip = new Set(['node_modules', '.nuxt', '.output', '.data', 'dist'])

function folders(dir: string): string[] {
  return readdirSync(dir)
    .filter(name => !skip.has(name) && statSync(join(dir, name)).isDirectory())
    .flatMap(name => [join(dir, name), ...folders(join(dir, name))])
}

describe('source folders', () => {
  it('are never named releases (the root .gitignore drops them, and Tailwind skips them)', () => {
    expect(folders(root).filter(path => /[\\/][Rr]eleases$/.test(path))).toEqual([])
  })
})
