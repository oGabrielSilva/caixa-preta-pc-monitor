import { describe, expect, it } from 'vitest'
import { summarize } from '~/utils/incident-stats'
import { incident } from '~~/test/support/fixtures'

const now = new Date('2026-11-24T18:00:00Z')

describe('summarize', () => {
  const incidents = [
    incident({ id: 4, loggedAt: '2026-11-24T13:40:00Z', estimatedFailureAt: '2026-11-24T13:30:00Z', observations: ['boot.early_failure'] }),
    incident({ id: 3, loggedAt: '2026-11-23T22:10:00Z', sleepState: 4 }),
    incident({ id: 2, loggedAt: '2026-11-18T10:00:00Z' }),
    incident({ id: 1, loggedAt: '2026-11-16T10:00:00Z' }),
  ]

  it('counts early boot, sleep and the last 24 hours', () => {
    const stats = summarize(incidents, now)

    expect([stats.total, stats.last24h, stats.earlyBoot, stats.duringSleep]).toEqual([4, 2, 1, 1])
  })

  it('fills days without incidents with zero', () => {
    const perDay = summarize(incidents, now).perDay

    expect(perDay.map(d => d.count)).toEqual([1, 0, 1, 0, 0, 0, 0, 1, 1])
    expect(perDay[0]!.day).toBe('2026-11-16')
  })

  it('is empty without incidents', () => {
    expect(summarize([], now)).toEqual({ total: 0, last24h: 0, earlyBoot: 0, duringSleep: 0, perDay: [] })
  })
})
