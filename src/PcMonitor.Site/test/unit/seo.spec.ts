import { describe, expect, it } from 'vitest'
import raw from '../fixtures/github-releases.json'
import { message } from '../support/locales'
import { FAQ_IDS, faqKeys } from '~/utils/faq'
import { latestStable, mapReleases } from '~~/shared/utils/releases'
import { absoluteUrl, breadcrumbJsonLd, headLinks, faqPageJsonLd, robotsTxt, sitemapXml, softwareApplicationJsonLd, websiteJsonLd } from '~~/shared/utils/seo'

const base = 'https://ogabrielsilva.github.io/caixa-preta-pc-monitor/'

describe('absoluteUrl', () => {
  it.each([
    ['/', base],
    ['', base],
    ['/en', `${base}en`],
    ['/en/releases', `${base}en/releases`],
    ['og/og-pt-BR.png', `${base}og/og-pt-BR.png`],
  ])('%s → %s (the project path exactly once)', (path, url) => {
    expect(absoluteUrl(path)).toBe(url)
  })

  it('does not add the project path twice', () => {
    expect(absoluteUrl('/caixa-preta-pc-monitor/versoes')).toBe(`${base}versoes`)
  })
})

describe('softwareApplicationJsonLd', () => {
  const input = { description: 'Monitor', locale: 'pt-BR', screenshots: [`${base}screens/overview-1600.webp`] }

  it('describes a free Windows 11 utility under the MIT licence by Gabriel Silva', () => {
    expect(softwareApplicationJsonLd({ ...input, release: null })).toMatchObject({
      '@context': 'https://schema.org',
      '@type': 'SoftwareApplication',
      'name': 'Caixa-Preta',
      'operatingSystem': 'Windows 11',
      'applicationCategory': 'UtilitiesApplication',
      'offers': { '@type': 'Offer', 'price': '0' },
      'license': 'https://opensource.org/licenses/MIT',
      'author': { '@type': 'Person', 'name': 'Gabriel Silva', 'url': 'https://github.com/oGabrielSilva' },
      'inLanguage': 'pt-BR',
    })
  })

  it('names version, download and size only when a release exists', () => {
    expect(softwareApplicationJsonLd({ ...input, release: null })).not.toHaveProperty('softwareVersion')
    expect(softwareApplicationJsonLd({ ...input, release: latestStable(mapReleases(raw)) })).toMatchObject({
      softwareVersion: '1.1.0',
      downloadUrl: 'https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases/download/v1.1.0/CaixaPreta-Setup-1.1.0.exe',
      datePublished: '2026-12-10T08:00:00Z',
      fileSize: '58 MB',
    })
  })

  it('never carries an e-mail address', () => {
    expect(JSON.stringify(softwareApplicationJsonLd({ ...input, release: null }))).not.toMatch(/@\w+\.\w+/)
  })
})

describe('faqPageJsonLd', () => {
  it('has one answered Question per item', () => {
    const json = faqPageJsonLd([{ question: 'Q1?', answer: 'A1.' }, { question: 'Q2?', answer: 'A2.' }]) as { mainEntity: unknown[] }
    expect(json).toMatchObject({ '@type': 'FAQPage' })
    expect(json.mainEntity).toEqual([
      { '@type': 'Question', 'name': 'Q1?', 'acceptedAnswer': { '@type': 'Answer', 'text': 'A1.' } },
      { '@type': 'Question', 'name': 'Q2?', 'acceptedAnswer': { '@type': 'Answer', 'text': 'A2.' } },
    ])
  })

  it.each(['pt-BR', 'en'] as const)('every FAQ question and answer is written in %s', (code) => {
    expect(FAQ_IDS.length).toBeGreaterThanOrEqual(8)
    for (const id of FAQ_IDS) {
      const keys = faqKeys(id)
      expect(message(code, keys.question), keys.question).toBeTruthy()
      expect(message(code, keys.answer), keys.answer).toBeTruthy()
    }
  })
})

describe('websiteJsonLd and breadcrumbJsonLd', () => {
  it('point at absolute URLs', () => {
    expect(websiteJsonLd('en')).toMatchObject({ '@type': 'WebSite', 'url': base, 'inLanguage': 'en' })
    expect(breadcrumbJsonLd([{ name: 'Início', path: '/' }, { name: 'Versões', path: '/versoes' }])).toMatchObject({
      '@type': 'BreadcrumbList',
      'itemListElement': [
        { '@type': 'ListItem', 'position': 1, 'name': 'Início', 'item': base },
        { '@type': 'ListItem', 'position': 2, 'name': 'Versões', 'item': `${base}versoes` },
      ],
    })
  })
})

describe('sitemapXml', () => {
  const xml = sitemapXml([{ path: '/', alternates: { 'pt-BR': '/', 'en': '/en' } }])

  it('lists each page with its language alternates and x-default', () => {
    expect(xml).toContain(`<loc>${base}</loc>`)
    expect(xml).toContain(`<xhtml:link rel="alternate" hreflang="en" href="${base}en"/>`)
    expect(xml).toContain(`<xhtml:link rel="alternate" hreflang="pt-BR" href="${base}"/>`)
    expect(xml).toContain(`<xhtml:link rel="alternate" hreflang="x-default" href="${base}"/>`)
    expect(xml.startsWith('<?xml version="1.0" encoding="UTF-8"?>')).toBe(true)
  })
})

describe('headLinks', () => {
  it('gives the canonical and one alternate per language plus x-default, all absolute', () => {
    expect(headLinks('/caixa-preta-pc-monitor/en/releases', { 'pt-BR': '/caixa-preta-pc-monitor/versoes', 'en': '/caixa-preta-pc-monitor/en/releases' })).toEqual([
      { rel: 'canonical', href: `${base}en/releases` },
      { rel: 'alternate', hreflang: 'pt-BR', href: `${base}versoes` },
      { rel: 'alternate', hreflang: 'en', href: `${base}en/releases` },
      { rel: 'alternate', hreflang: 'x-default', href: `${base}versoes` },
    ])
  })

  it('keeps the trailing slash only on the home page', () => {
    expect(headLinks('/', { 'pt-BR': '/', 'en': '/en' })[0]).toEqual({ rel: 'canonical', href: base })
  })
})

describe('robotsTxt', () => {
  it('allows everything and names the sitemap', () => {
    expect(robotsTxt()).toBe(`User-agent: *\nAllow: /\n\nSitemap: ${base}sitemap.xml\n`)
  })
})
