import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import type { Messages } from '../../../PcMonitor.Brand/shared/locale-keys'

/** The landing page texts of one language, read from disk (the i18n Vite plugin would compile them). */
export function siteMessages(code: 'pt-BR' | 'en'): Messages {
  return JSON.parse(readFileSync(fileURLToPath(new URL(`../../i18n/locales/${code}.json`, import.meta.url)), 'utf8')) as Messages
}

/** Reads one dotted key (`faq.free.q`) from a language. */
export function message(code: 'pt-BR' | 'en', key: string): string | undefined {
  let node: string | Messages | undefined = siteMessages(code)
  for (const part of key.split('.')) {
    node = typeof node === 'object' ? node[part] : undefined
  }
  return typeof node === 'string' ? node : undefined
}
