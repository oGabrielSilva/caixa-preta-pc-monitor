// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import IncidentTimeline from '~/components/incidents/IncidentTimeline.vue'
import { mountPtBR } from '~~/test/support/nuxt'
import { incident } from '~~/test/support/fixtures'

describe('IncidentTimeline', () => {
  it('orders the timeline by time and names the windows events', async () => {
    const timeline = await mountPtBR(IncidentTimeline, { props: { detail: {
      summary: incident({ loggedAt: '2026-11-24T20:58:12Z', estimatedFailureAt: '2026-11-24T20:56:41Z', timeSource: 'LastFrame', hasTelemetry: true }),
      lastAliveAt: '2026-11-24T20:34:02Z',
      telemetryEndAt: '2026-11-24T20:56:41Z',
      events: [
        { provider: 'EventLog', eventId: 6008, loggedAt: '2026-11-24T20:58:23Z', level: 2 },
        { provider: 'EventLog', eventId: 6005, loggedAt: '2026-11-24T20:58:01Z', level: 4 },
      ],
      diagnosis: null,
      telemetry: null,
    } } })

    const text = timeline.text()
    const order = ['Último sinal de vida (Windows)', 'Última leitura da Caixa-Preta', 'Windows iniciou', 'Windows registrou o desligamento inesperado', 'O desligamento anterior foi inesperado']
    expect(order.map(title => text.indexOf(title))).toEqual([...order.map(title => text.indexOf(title))].sort((a, b) => a - b))
    expect(order.every(title => text.includes(title))).toBe(true)
  })
  it('shows the shutdown event 41 once', async () => {
    const timeline = await mountPtBR(IncidentTimeline, { props: { detail: {
      summary: incident({ loggedAt: '2026-11-24T16:54:38Z' }),
      lastAliveAt: null,
      telemetryEndAt: null,
      events: [{ provider: 'Microsoft-Windows-Kernel-Power', eventId: 41, loggedAt: '2026-11-24T16:54:38Z', level: 1 }],
      diagnosis: null,
      telemetry: null,
    } } })

    expect(timeline.text().match(/Kernel-Power 41/g)).toHaveLength(1)
  })
})
