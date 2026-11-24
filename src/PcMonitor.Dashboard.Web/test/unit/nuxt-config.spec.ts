import { describe, expect, it, vi } from 'vitest'

// nuxt.config.ts calls the auto-imported defineNuxtConfig; outside Nuxt it is the identity function.
vi.stubGlobal('defineNuxtConfig', (config: unknown) => config)

describe('nuxt.config', () => {
  it('bundles icons at build time and never calls the Iconify API', async () => {
    const { default: config } = await import('~~/nuxt.config')

    expect(config).toMatchObject({ icon: { provider: 'none', fallbackToApi: false } })
  })
})
