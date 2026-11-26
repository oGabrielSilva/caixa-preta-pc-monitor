// Absolute URLs and machine-readable descriptions of the landing page (SEO, AEO, GEO): JSON-LD, sitemap, robots.
import type { Release } from './releases'
import { AUTHOR, BASE_PATH, REPO_URL, SITE_URL } from './site'

/** Absolute URL of a site path. Accepts paths with or without the project path; the result has it exactly once. */
export function absoluteUrl(path: string): string {
  const relative = path.replace(/^\/+/, '').replace(new RegExp(`^${BASE_PATH.slice(1)}`), '')
  return `${SITE_URL}${relative}`
}

export type HeadLink = { rel: 'canonical', href: string } | { rel: 'alternate', hreflang: string, href: string }

/**
 * Canonical and hreflang links of a page. Built here rather than by @nuxtjs/i18n, which repeats the project path when
 * the site lives under a sub-path. pt-BR is the x-default.
 * @param path Current route path. @param alternates Path of the same page per language.
 */
export function headLinks(path: string, alternates: Record<string, string>): HeadLink[] {
  return [
    { rel: 'canonical', href: absoluteUrl(path) },
    ...Object.entries(alternates).map(([hreflang, href]) => ({ rel: 'alternate' as const, hreflang, href: absoluteUrl(href) })),
    { rel: 'alternate', hreflang: 'x-default', href: absoluteUrl(alternates['pt-BR'] ?? path) },
  ]
}

/** File size in MB (MiB, as Windows shows it), e.g. `58 MB`. */
export function megabytes(bytes: number): string {
  return `${Math.round(bytes / 1_048_576)} MB`
}

export interface SoftwareApplicationInput {
  description: string
  locale: string
  release: Release | null
  /** Absolute image URLs. */
  screenshots: string[]
}

export function softwareApplicationJsonLd({ description, locale, release, screenshots }: SoftwareApplicationInput): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    'name': 'Caixa-Preta',
    description,
    'url': SITE_URL,
    'inLanguage': locale,
    'operatingSystem': 'Windows 11',
    'applicationCategory': 'UtilitiesApplication',
    'offers': { '@type': 'Offer', 'price': '0', 'priceCurrency': 'BRL' },
    'license': 'https://opensource.org/licenses/MIT',
    'isAccessibleForFree': true,
    'author': { '@type': 'Person', ...AUTHOR },
    'codeRepository': REPO_URL,
    'sameAs': [REPO_URL],
    'image': absoluteUrl('/icon.svg'),
    'screenshot': screenshots,
    ...(release?.installer
      ? {
          softwareVersion: release.version,
          downloadUrl: release.installer.url,
          datePublished: release.publishedAt,
          fileSize: megabytes(release.installer.size),
        }
      : {}),
  }
}

export function faqPageJsonLd(items: { question: string, answer: string }[]): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    'mainEntity': items.map(({ question, answer }) => ({
      '@type': 'Question',
      'name': question,
      'acceptedAnswer': { '@type': 'Answer', 'text': answer },
    })),
  }
}

export function websiteJsonLd(locale: string): object {
  return { '@context': 'https://schema.org', '@type': 'WebSite', 'name': 'Caixa-Preta', 'url': SITE_URL, 'inLanguage': locale }
}

export function breadcrumbJsonLd(items: { name: string, path: string }[]): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    'itemListElement': items.map(({ name, path }, index) => ({ '@type': 'ListItem', 'position': index + 1, name, 'item': absoluteUrl(path) })),
  }
}

export interface SitemapPage {
  path: string
  /** Path of the same page in each language (`pt-BR`, `en`); pt-BR is also the x-default. */
  alternates: Record<string, string>
}

export function sitemapXml(pages: SitemapPage[]): string {
  const urls = pages.map((page) => {
    const links = Object.entries(page.alternates).map(([lang, path]) => `<xhtml:link rel="alternate" hreflang="${lang}" href="${absoluteUrl(path)}"/>`)
    const fallback = page.alternates['pt-BR'] ?? page.path
    return [
      '  <url>',
      `    <loc>${absoluteUrl(page.path)}</loc>`,
      ...links.map(link => `    ${link}`),
      `    <xhtml:link rel="alternate" hreflang="x-default" href="${absoluteUrl(fallback)}"/>`,
      '  </url>',
    ].join('\n')
  })
  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">',
    ...urls,
    '</urlset>',
    '',
  ].join('\n')
}

export function robotsTxt(): string {
  return `User-agent: *\nAllow: /\n\nSitemap: ${absoluteUrl('/sitemap.xml')}\n`
}

/** Every page of the site, in both languages (sitemap and prerender). */
export const SITE_PAGES: SitemapPage[] = [
  { path: '/', alternates: { 'pt-BR': '/', 'en': '/en' } },
  { path: '/en', alternates: { 'pt-BR': '/', 'en': '/en' } },
  { path: '/versoes', alternates: { 'pt-BR': '/versoes', 'en': '/en/releases' } },
  { path: '/en/releases', alternates: { 'pt-BR': '/versoes', 'en': '/en/releases' } },
]
