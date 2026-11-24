import { describe, expect, it } from 'vitest'
import config from '~~/nuxt.config'

describe('nuxt.config', () => {
  it('bundles icons at build time and never calls the Iconify API', () => {
    expect(config).toMatchObject({ icon: { provider: 'none', fallbackToApi: false } })
  })
})
