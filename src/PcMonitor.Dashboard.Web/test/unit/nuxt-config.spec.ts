import { describe, expect, it } from 'vitest'
import brandConfig from '../../../PcMonitor.Brand/nuxt.config'

describe('nuxt.config', () => {
  it('bundles icons at build time and never calls the Iconify API (Brand layer)', () => {
    expect(brandConfig).toMatchObject({ icon: { provider: 'none', fallbackToApi: false } })
  })
})
