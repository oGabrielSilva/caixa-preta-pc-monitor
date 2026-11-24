import type { FramePoint, LiveResponse, SensorResponse } from '~/api/types'

/** The last minutes of the segment being recorded, as the live page shows them. */
export interface LiveBuffer {
  readonly segmentId: number | null
  readonly sensors: readonly SensorResponse[]
  readonly frames: readonly FramePoint[]
  readonly windowMs: number
}

export interface SeriesPoint {
  readonly t: number
  readonly v: number | null
}

export const liveWindowMs = 10 * 60 * 1000

export function createLiveBuffer(snapshot: LiveResponse, windowMs = liveWindowMs): LiveBuffer {
  return { segmentId: snapshot.segmentId, sensors: snapshot.sensors, frames: snapshot.frames, windowMs }
}

/** Adds frames newer than the last one and drops those older than the window. Frames of another segment are ignored. */
export function appendFrames(buffer: LiveBuffer, segmentId: number | null | undefined, frames: readonly FramePoint[]): LiveBuffer {
  if (segmentId !== buffer.segmentId) {
    return buffer
  }

  const last = buffer.frames.at(-1)?.t ?? Number.NEGATIVE_INFINITY
  const merged = [...buffer.frames, ...frames.filter(f => f.t > last)]
  const newest = merged.at(-1)?.t ?? 0
  return { ...buffer, frames: merged.filter(f => f.t > newest - buffer.windowMs || f.t === newest) }
}

function ordinalOf(buffer: LiveBuffer, sensorKey: string): number {
  return buffer.sensors.findIndex(s => s.key === sensorKey)
}

export function seriesOf(buffer: LiveBuffer, sensorKey: string): SeriesPoint[] {
  const ordinal = ordinalOf(buffer, sensorKey)
  return ordinal < 0 ? [] : buffer.frames.map(f => ({ t: f.t, v: f.values[ordinal] ?? null }))
}

export function latestValue(buffer: LiveBuffer, sensorKey: string): number | null {
  const ordinal = ordinalOf(buffer, sensorKey)
  return ordinal < 0 ? null : buffer.frames.at(-1)?.values[ordinal] ?? null
}

export function sensorByRole(buffer: LiveBuffer, role: SensorResponse['role']): SensorResponse | undefined {
  return buffer.sensors.find(s => s.role === role)
}
