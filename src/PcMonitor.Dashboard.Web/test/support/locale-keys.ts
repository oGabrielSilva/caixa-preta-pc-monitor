import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

type Messages = { readonly [key: string]: string | Messages }

/**
 * Reads a locale file straight from disk. Importing it would go through the i18n Vite plugin,
 * which compiles messages into another shape.
 */
export function loadLocale(code: 'pt-BR' | 'en'): Messages {
  const path = fileURLToPath(new URL(`../../i18n/locales/${code}.json`, import.meta.url))
  return JSON.parse(readFileSync(path, 'utf8')) as Messages
}

/** Flattens nested i18n messages into dot-separated keys, e.g. `nav.live`. */
export function localeKeys(messages: Messages, prefix = ''): string[] {
  return Object.entries(messages).flatMap(([key, value]) =>
    typeof value === 'string' ? [`${prefix}${key}`] : localeKeys(value, `${prefix}${key}.`),
  )
}
