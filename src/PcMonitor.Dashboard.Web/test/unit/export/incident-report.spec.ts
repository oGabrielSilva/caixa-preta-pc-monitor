import { describe, expect, it } from 'vitest'
import type { IncidentDetail } from '~/api/types'
import { incidentReport } from '~/utils/export/incident-report'
import { telemetrySvg } from '~/utils/export/telemetry-svg'
import { incident } from '~~/test/support/fixtures'

const t = (key: string, named?: Record<string, unknown>) => (named ? `${key}${JSON.stringify(named)}` : key)

function detail(overrides: Partial<IncidentDetail>): IncidentDetail {
  return { summary: incident({ id: 17, topHypothesis: 'BoardPowerDelivery', confidence: 'Medium' }), lastAliveAt: null, telemetryEndAt: null, events: [], diagnosis: null, telemetry: null, ...overrides }
}

const text = (value: unknown) => JSON.stringify(value)

describe('incidentReport', () => {
  it('puts the translated findings and the telemetry chart in the incident report', () => {
    const report = incidentReport(detail({
      diagnosis: {
        engineVersion: 1, diagnosedAt: '2026-11-24T21:00:00Z',
        ranking: [{ hypothesis: 'BoardPowerDelivery', score: 3 }],
        findings: [{ code: 'load.idle', kind: 'Evidence', hypothesis: 'BoardPowerDelivery', supports: true, strength: 'Weak', parameters: { avg: '6.6' } }],
        limitations: [{ code: 'rail.precision', kind: 'Limitation', hypothesis: null, supports: true, strength: 'Weak', parameters: {} }],
      },
      telemetry: { endAt: '2026-11-24T20:56:41Z', atFailure: true, series: [
        { role: 'Rail12V', unit: 'V', range: { min: 11.4, max: 12.6 }, limit: null, critical: null, points: [{ t: 0, v: 12.19 }, { t: 1000, v: 11.28 }] },
      ] },
    }), t, 'pt-BR')

    expect(text(report)).toContain('findings.load_idle{\\"avg\\":\\"6,6\\"}')
    expect(text(report)).toContain('findings.rail_precision')
    expect(text(report)).toContain('<svg')
  })

  it('omits the chart and says so when there is no telemetry', () => {
    const report = incidentReport(detail({}), t, 'pt-BR')

    expect(text(report)).not.toContain('<svg')
    expect(text(report)).toContain('incident.telemetry.none')
  })
})

describe('telemetrySvg', () => {
  it('draws the range band and one point per reading', () => {
    const svg = telemetrySvg([{ t: 0, v: 12 }, { t: 1000, v: 11.3 }, { t: 2000, v: 12.1 }], { min: 11.4, max: 12.6 })

    expect(svg).toMatch(/^<svg /)
    expect(svg).toContain('<rect')
    expect(svg.match(/<polyline points="([^"]+)"/)![1]!.trim().split(' ')).toHaveLength(3)
  })
})
