import { describe, expect, it } from 'vitest'
import config from '~~/nuxt.config'

describe('site nuxt.config', () => {
  it('is served from the GitHub Pages project path', () => {
    expect(config.app?.baseURL).toBe('/caixa-preta-pc-monitor/')
  })

  it('renders every page to HTML at build time (SEO)', () => {
    expect(config.ssr).toBe(true)
  })

  it('keeps pt-BR at / and puts English under /en/', () => {
    expect(config.i18n).toMatchObject({ strategy: 'prefix_except_default', defaultLocale: 'pt-BR' })
  })

  it('scrolls smoothly to the section a menu link points at', () => {
    expect(config.router?.options?.scrollBehaviorType).toBe('smooth')
  })

  it('shares the theme with the dashboard', () => {
    expect(config.extends).toEqual(['../PcMonitor.Brand'])
  })
})
