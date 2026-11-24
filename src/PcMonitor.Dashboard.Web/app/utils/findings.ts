import type { Finding, FindingCode, Hypothesis } from '~/api/types'

export type Translate = (key: string, named?: Record<string, unknown>) => string

/** One finding as shown: the same code and parameters for several hypotheses become one line. */
export interface GroupedFinding {
  readonly code: FindingCode
  readonly kind: Finding['kind']
  readonly supports: boolean
  readonly strength: Finding['strength']
  readonly hypotheses: readonly Hypothesis[]
  readonly parameters: Readonly<Record<string, string>>
}

function identity(finding: Finding): string {
  const parameters = Object.entries(finding.parameters).sort(([a], [b]) => a.localeCompare(b))
  return JSON.stringify([finding.code, finding.kind, finding.supports, parameters])
}

export function groupFindings(findings: readonly Finding[]): GroupedFinding[] {
  const groups = new Map<string, GroupedFinding & { hypotheses: Hypothesis[] }>()
  for (const finding of findings) {
    const key = identity(finding)
    const group = groups.get(key) ?? { ...finding, hypotheses: [] }
    if (finding.hypothesis) {
      group.hypotheses.push(finding.hypothesis)
    }
    groups.set(key, group)
  }
  return [...groups.values()]
}

const invariantNumber = /^-?\d+(\.\d+)?$/

/** `12V` → `+12 V`, `3.3V` → `+3,3 V` (pt-BR). */
function railName(rail: string, locale: string): string {
  const volts = Number.parseFloat(rail)
  return Number.isNaN(volts) ? rail : `+${volts.toLocaleString(locale)} V`
}

function wheaSources(sources: string, t: Translate): string {
  return sources.split(',').map((source) => {
    const [kind, value] = source.split(':')
    switch (kind) {
      case 'bugcheck': return t('whea.bugcheck', { code: value })
      case 'event': return t('whea.event', { id: value })
      case 'boot_errors': return t('whea.bootErrors', { count: value })
      default: return source
    }
  }).join(', ')
}

/** Turns stored parameters (invariant strings) into display text in `locale`. */
export function findingParams(parameters: Readonly<Record<string, string>>, locale: string, t: Translate): Record<string, string> {
  return Object.fromEntries(Object.entries(parameters).map(([name, value]) => {
    switch (name) {
      case 'rail': return [name, railName(value, locale)]
      case 'rails': return [name, value.split(',').map(rail => railName(rail, locale)).join(', ')]
      case 'role': return [name, t(`roles.${value}`)]
      case 'state': return [name, t(`sleepStates.${value}`)]
      case 'sources': return [name, wheaSources(value, t)]
      case 'code': return [name, value]
      default: return [name, invariantNumber.test(value) ? Number(value).toLocaleString(locale, { maximumFractionDigits: 3 }) : value]
    }
  }))
}
