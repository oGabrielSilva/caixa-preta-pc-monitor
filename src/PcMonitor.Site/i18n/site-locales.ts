/** Languages of the landing page: Portuguese at `/`, English under `/en/`. Used by nuxt.config (i18n) and the SEO tags. */
export const siteLocales = [
  { code: 'pt-BR', language: 'pt-BR', name: 'Português', file: 'pt-BR.json' },
  { code: 'en', language: 'en-US', name: 'English', file: 'en.json' },
] as const

export type SiteLocaleCode = (typeof siteLocales)[number]['code']

export const defaultLocale: SiteLocaleCode = 'pt-BR'
