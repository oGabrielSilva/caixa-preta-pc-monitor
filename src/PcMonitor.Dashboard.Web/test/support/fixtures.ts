import type { FramePoint, IncidentSummary, LiveResponse, SensorResponse } from '~/api/types'

export const rail12: SensorResponse = {
  key: '/lpc/nct6799d/0/voltage/4', name: '+12V', kind: 'Voltage', unit: 'V', role: 'Rail12V',
  range: { min: 11.4, max: 12.6 }, limit: null, critical: null, storage: null,
}

export const cpuTemperature: SensorResponse = {
  key: '/amdcpu/0/temperature/2', name: 'CPU (Tctl/Tdie)', kind: 'Temperature', unit: '°C', role: 'CpuTemperature',
  range: null, limit: 95, critical: 100, storage: null,
}

export const fan: SensorResponse = {
  key: '/lpc/nct6799d/0/fan/1', name: 'Fan #2', kind: 'Fan', unit: 'RPM', role: 'None', range: null, limit: null, critical: null, storage: null,
}

/** One frame per second from `start` (Unix ms) with +12 V and CPU temperature values. */
export function frames(start: number, values: [number | null, number | null][]): FramePoint[] {
  return values.map((pair, i) => ({ t: start + i * 1000, values: pair }))
}

export function liveSnapshot(framePoints: FramePoint[], segmentId = 7): LiveResponse {
  return {
    state: 'Recording',
    session: { startedAt: '2026-11-24T17:00:00Z', sourceStatus: 'Ok', boardProduct: 'TUF GAMING A620M-PLUS WIFI' },
    segmentId,
    sensors: [rail12, cpuTemperature],
    frames: framePoints,
  }
}

export function incident(overrides: Partial<IncidentSummary>): IncidentSummary {
  return {
    id: 1,
    loggedAt: '2026-11-24T13:00:00Z',
    estimatedFailureAt: null,
    timeSource: 'LastAliveEventLog',
    sleepState: 0,
    bugcheckCode: 0,
    hasTelemetry: false,
    topHypothesis: null,
    confidence: 'Inconclusive',
    observations: [],
    ...overrides,
  }
}
