import { existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import config from '~~/nuxt.config'
import brandAppConfig from '../../../PcMonitor.Brand/app/app.config'

const file = (path: string) => fileURLToPath(new URL(path, import.meta.url))

describe('brand layer', () => {
  it('is extended by the dashboard (one theme for the dashboard and the landing page)', () => {
    expect(config.extends).toEqual(['../PcMonitor.Brand'])
  })

  it('holds the theme chosen by the owner (ADR 0015)', () => {
    expect(brandAppConfig.ui?.colors).toMatchObject({ primary: 'teal', neutral: 'neutral' })
  })

  it('is the only copy of the theme', () => {
    expect(existsSync(file('../../app/app.config.ts'))).toBe(false)
  })
})
