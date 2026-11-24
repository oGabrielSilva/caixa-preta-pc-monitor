/**
 * Languages the dashboard ships. Single source for the i18n module (nuxt.config.ts)
 * and for the Nuxt UI locale mapping (app/utils/ui-locale.ts).
 */
export const appLocales = [
  { code: 'pt-BR', language: 'pt-BR', name: 'Português', file: 'pt-BR.json' },
  { code: 'en', language: 'en-US', name: 'English', file: 'en.json' },
] as const

export type AppLocaleCode = (typeof appLocales)[number]['code']

export const defaultLocale: AppLocaleCode = 'pt-BR'
