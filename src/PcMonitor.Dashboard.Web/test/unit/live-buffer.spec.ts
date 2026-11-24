import { describe, expect, it } from 'vitest'
import { appendFrames, createLiveBuffer, latestValue, seriesOf } from '~/utils/live-buffer'
import { frames, liveSnapshot, rail12 } from '~~/test/support/fixtures'

const t0 = Date.UTC(2026, 10, 24, 17, 0, 0)

describe('live buffer', () => {
  it('keeps the snapshot frames and appends newer ones once', () => {
    const buffer = createLiveBuffer(liveSnapshot(frames(t0, [[12.19, 45], [12.2, 46]])))

    const next = appendFrames(buffer, 7, frames(t0 + 1000, [[12.2, 46], [12.18, 47]]))

    expect(next.frames.map(f => f.t)).toEqual([t0, t0 + 1000, t0 + 2000])
  })

  it('drops frames older than the window', () => {
    const buffer = createLiveBuffer(liveSnapshot(frames(t0, [[12.19, 45]])), 2000)

    const next = appendFrames(buffer, 7, frames(t0 + 3000, [[12.2, 46]]))

    expect(next.frames.map(f => f.t)).toEqual([t0 + 3000])
  })

  it('ignores frames of another segment', () => {
    const buffer = createLiveBuffer(liveSnapshot(frames(t0, [[12.19, 45]])))

    expect(appendFrames(buffer, 8, frames(t0 + 1000, [[12.2, 46]]))).toBe(buffer)
  })

  it('returns the series and the latest value of a sensor', () => {
    const buffer = createLiveBuffer(liveSnapshot(frames(t0, [[12.19, 45], [12.2, null]])))

    expect(seriesOf(buffer, rail12.key)).toEqual([{ t: t0, v: 12.19 }, { t: t0 + 1000, v: 12.2 }])
    expect(latestValue(buffer, '/amdcpu/0/temperature/2')).toBeNull()
    expect(latestValue(buffer, '/unknown')).toBeNull()
  })
})
