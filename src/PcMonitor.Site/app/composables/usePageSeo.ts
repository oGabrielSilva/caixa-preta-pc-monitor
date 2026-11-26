import { siteLocales } from '~~/i18n/site-locales'
import { absoluteUrl, headLinks } from '~~/shared/utils/seo'

export interface PageSeo {
  /** i18n key of the page title (also og:title). */
  titleKey: string
  /** i18n key of the meta description (also og:description). */
  descriptionKey: string
  /** JSON-LD objects for this page. */
  jsonLd?: () => object[]
}

/**
 * Title, description, Open Graph and Twitter card, canonical and hreflang links (absolute, see headLinks) and the
 * page's JSON-LD.
 */
export function usePageSeo({ titleKey, descriptionKey, jsonLd }: PageSeo): void {
  const { t, locale } = useI18n()
  const route = useRoute()
  const switchLocalePath = useSwitchLocalePath()

  const image = () => absoluteUrl(`/og/og-${locale.value}.png`)
  const url = () => absoluteUrl(route.path)

  useSeoMeta({
    title: () => t(titleKey),
    description: () => t(descriptionKey),
    ogTitle: () => t(titleKey),
    ogDescription: () => t(descriptionKey),
    ogType: 'website',
    ogSiteName: 'Caixa-Preta',
    ogUrl: url,
    ogImage: image,
    ogImageWidth: 1200,
    ogImageHeight: 630,
    ogImageAlt: () => t(titleKey),
    ogLocale: () => (locale.value === 'en' ? 'en_US' : 'pt_BR'),
    ogLocaleAlternate: () => [locale.value === 'en' ? 'pt_BR' : 'en_US'],
    twitterCard: 'summary_large_image',
    twitterTitle: () => t(titleKey),
    twitterDescription: () => t(descriptionKey),
    twitterImage: image,
  })

  useHead(() => ({
    link: headLinks(route.path, Object.fromEntries(siteLocales.map(({ code }) => [code, switchLocalePath(code)]))),
    script: (jsonLd?.() ?? []).map(json => ({ type: 'application/ld+json', innerHTML: JSON.stringify(json) })),
  }))
}
