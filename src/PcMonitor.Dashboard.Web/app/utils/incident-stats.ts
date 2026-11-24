import type { IncidentSummary } from '~/api/types'

export interface IncidentStats {
  readonly total: number
  readonly last24h: number
  readonly earlyBoot: number
  readonly duringSleep: number
  /** One entry per local day from the first incident to today, oldest first. */
  readonly perDay: readonly { readonly day: string, readonly count: number }[]
}

const dayMs = 24 * 60 * 60 * 1000

/** When the machine went down: the estimate when there is one, else when Windows logged it. */
export function failureTime(incident: IncidentSummary): Date {
  return new Date(incident.estimatedFailureAt ?? incident.loggedAt)
}

/** Local calendar day as `YYYY-MM-DD`. */
export function localDay(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function summarize(incidents: readonly IncidentSummary[], now: Date): IncidentStats {
  const times = incidents.map(failureTime)
  const counts = new Map<string, number>()
  for (const time of times) {
    counts.set(localDay(time), (counts.get(localDay(time)) ?? 0) + 1)
  }

  const perDay: { day: string, count: number }[] = []
  if (times.length > 0) {
    const first = new Date(Math.min(...times.map(t => t.getTime())))
    const cursor = new Date(first.getFullYear(), first.getMonth(), first.getDate())
    while (localDay(cursor) <= localDay(now)) {
      perDay.push({ day: localDay(cursor), count: counts.get(localDay(cursor)) ?? 0 })
      cursor.setDate(cursor.getDate() + 1)
    }
  }

  return {
    total: incidents.length,
    last24h: times.filter(t => now.getTime() - t.getTime() <= dayMs).length,
    earlyBoot: incidents.filter(i => i.observations.includes('boot.early_failure')).length,
    duringSleep: incidents.filter(i => i.sleepState !== 0).length,
    perDay,
  }
}
