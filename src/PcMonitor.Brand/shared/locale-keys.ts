// Checks shared by the dashboard and landing page locale tests: both apps keep pt-BR and en in step.

export type Messages = { readonly [key: string]: string | Messages }

/** Flattens nested i18n messages into dot-separated keys, e.g. `nav.live`. */
export function localeKeys(messages: Messages, prefix = ''): string[] {
  return Object.entries(messages).flatMap(([key, value]) =>
    typeof value === 'string' ? [`${prefix}${key}`] : localeKeys(value, `${prefix}${key}.`),
  )
}

/** Keys that contain a dot themselves (vue-i18n would read the dot as nesting). */
export function dottedKeys(messages: Messages): string[] {
  return Object.entries(messages).flatMap(([key, value]) => [
    ...(key.includes('.') ? [key] : []),
    ...(typeof value === 'string' ? [] : dottedKeys(value)),
  ])
}

/** Every text value, at any depth. */
export function localeValues(messages: Messages): string[] {
  return Object.values(messages).flatMap(value => (typeof value === 'string' ? [value] : localeValues(value)))
}
