// The questions of the FAQ, in the order shown. The single source for the visible FAQ and the FAQPage JSON-LD.
export const FAQ_IDS = [
  'whyShutdown',
  'what',
  'cause',
  'free',
  'privacy',
  'smartscreen',
  'sensors',
  'requirements',
  'uninstall',
  'contribute',
] as const

export type FaqId = (typeof FAQ_IDS)[number]

/** The i18n keys of one question and its answer. */
export function faqKeys(id: FaqId): { question: string, answer: string } {
  return { question: `faq.items.${id}.q`, answer: `faq.items.${id}.a` }
}
