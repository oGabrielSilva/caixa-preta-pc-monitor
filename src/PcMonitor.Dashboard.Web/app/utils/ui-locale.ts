import type { Locale, Messages } from '@nuxt/ui'
import { en, pt_br } from '@nuxt/ui/locale'
import { defaultLocale, type AppLocaleCode } from '~~/i18n/app-locales'

// A Record keyed by AppLocaleCode: adding a language without its Nuxt UI texts fails to compile.
const uiLocales: Record<AppLocaleCode, Locale<Messages>> = {
  'pt-BR': pt_br,
  'en': en,
}

/** Nuxt UI's own texts (tooltips, aria labels) for the active i18n locale. */
export function uiLocaleFor(code: string): Locale<Messages> {
  return uiLocales[code as AppLocaleCode] ?? uiLocales[defaultLocale]
}
