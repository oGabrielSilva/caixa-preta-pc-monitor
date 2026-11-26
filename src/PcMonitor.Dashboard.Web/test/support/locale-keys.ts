import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import type { Messages } from '../../../PcMonitor.Brand/shared/locale-keys'

export { localeKeys } from '../../../PcMonitor.Brand/shared/locale-keys'

/**
 * Reads a locale file straight from disk. Importing it would go through the i18n Vite plugin,
 * which compiles messages into another shape.
 */
export function loadLocale(code: 'pt-BR' | 'en'): Messages {
  const path = fileURLToPath(new URL(`../../i18n/locales/${code}.json`, import.meta.url))
  return JSON.parse(readFileSync(path, 'utf8')) as Messages
}
