// @vitest-environment nuxt
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import raw from '../fixtures/github-releases.json'
import DownloadButton from '~/components/shared/DownloadButton.vue'
import ScreenGallery from '~/components/home/ScreenGallery.vue'
import Home from '~/pages/index.vue'
import { FAQ_IDS } from '~/utils/faq'
import { mapReleases } from '~~/shared/utils/releases'

/** JSON-LD blocks the page put in <head>, after unhead flushed them. */
async function jsonLdTypes(): Promise<string[]> {
  await new Promise(resolve => setTimeout(resolve, 50))
  return [...document.head.querySelectorAll('script[type="application/ld+json"]')]
    .map(script => (JSON.parse(script.textContent ?? '{}') as { '@type': string })['@type'])
}

describe('home page', () => {
  it('has exactly one h1', async () => {
    const page = await mountSuspended(Home)
    expect(page.findAll('h1')).toHaveLength(1)
  })

  it('shows every FAQ question', async () => {
    const page = await mountSuspended(Home)
    expect(page.findAll('[data-faq-question]')).toHaveLength(FAQ_IDS.length)
  })

  it('describes the app, the site and the FAQ as JSON-LD', async () => {
    await mountSuspended(Home)
    expect(await jsonLdTypes()).toEqual(expect.arrayContaining(['WebSite', 'SoftwareApplication', 'FAQPage']))
  })

  it('tells why the app exists next to an illustration described for screen readers', async () => {
    const page = await mountSuspended(Home)
    const story = page.find('#historia')
    expect(story.find('h2').text()).toContain('gato')
    expect(story.find('svg[role="img"]').attributes('aria-label')).toContain('gato')
  })

  it('gives every image alt text and a size (no layout shift)', async () => {
    const page = await mountSuspended(Home)
    const images = page.findAll('img')
    expect(images.length).toBeGreaterThan(0)
    for (const image of images) {
      expect(image.attributes('alt'), image.attributes('src')).toBeTruthy()
      expect(image.attributes('width'), image.attributes('src')).toBeTruthy()
      expect(image.attributes('height'), image.attributes('src')).toBeTruthy()
    }
  })
})

describe('DownloadButton', () => {
  it('leads to the releases page, naming the latest stable version and its size', async () => {
    const button = await mountSuspended(DownloadButton, { props: { releases: mapReleases(raw) } })
    expect(button.find('a').attributes('href')).toBe('/caixa-preta-pc-monitor/versoes')
    expect(button.text()).toContain('1.1.0')
    expect(button.text()).toContain('58 MB')
  })

  it('leads to the releases page before the first release too', async () => {
    const button = await mountSuspended(DownloadButton, { props: { releases: [] } })
    expect(button.find('a').attributes('href')).toBe('/caixa-preta-pc-monitor/versoes')
  })
})

describe('ScreenGallery', () => {
  it('labels the demo-database screenshot as sample data', async () => {
    const gallery = await mountSuspended(ScreenGallery)
    expect(gallery.find('[data-screen="incident"]').text()).toContain('dados de exemplo')
    expect(gallery.find('[data-screen="overview"]').text()).not.toContain('dados de exemplo')
  })
})
