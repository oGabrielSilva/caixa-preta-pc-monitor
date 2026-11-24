import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { LiveResponse } from '~/api/types'
import type { ApiResult } from '~/composables/api-result'
import { createLiveStream, type EventSourceLike, type LiveStreamView } from '~/composables/live-stream'
import { frames, liveSnapshot } from '~~/test/support/fixtures'

const t0 = Date.UTC(2026, 10, 24, 17, 0, 0)

class FakeEventSource implements EventSourceLike {
  readyState = 1
  closed = false
  private readonly listeners = new Map<string, ((event: MessageEvent<string>) => void)[]>()

  constructor(readonly url: string) {}

  addEventListener(type: string, listener: (event: MessageEvent<string>) => void) {
    this.listeners.set(type, [...(this.listeners.get(type) ?? []), listener])
  }

  close() {
    this.closed = true
    this.readyState = 2
  }

  emit(type: string, data: unknown = {}) {
    for (const listener of this.listeners.get(type) ?? []) {
      listener({ data: JSON.stringify(data) } as MessageEvent<string>)
    }
  }
}

describe('createLiveStream', () => {
  let sources: FakeEventSource[]
  let results: ApiResult<LiveResponse>[]
  let view: LiveStreamView | undefined
  let load: ReturnType<typeof vi.fn<() => Promise<ApiResult<LiveResponse>>>>

  function start() {
    const stream = createLiveStream({
      load,
      open: (url) => {
        const source = new FakeEventSource(url)
        sources.push(source)
        return source
      },
      onChange: (next) => {
        view = next
      },
    })
    stream.start()
    return stream
  }

  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(t0 + 2000)
    sources = []
    view = undefined
    results = []
    load = vi.fn<() => Promise<ApiResult<LiveResponse>>>(async () => results.shift() ?? { data: liveSnapshot(frames(t0, [[12.19, 45], [12.2, 46]])), problem: null })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads the snapshot and opens the stream after its last frame', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    expect(view?.buffer?.frames).toHaveLength(2)
    expect(sources[0]!.url).toBe(`/api/live/stream?segmentId=7&after=${t0 + 1000}`)
  })

  it('appends frames from the stream', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    sources[0]!.emit('frames', { kind: 'Frames', segmentId: 7, frames: frames(t0 + 2000, [[12.18, 47]]), state: null })

    expect(view?.buffer?.frames.map(f => f.t)).toEqual([t0, t0 + 1000, t0 + 2000])
    expect(view?.state).toBe('Recording')
  })

  it('reloads the snapshot when the segment changes', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    sources[0]!.emit('segment', { kind: 'Segment', segmentId: 8, frames: null, state: null })
    await vi.advanceTimersByTimeAsync(0)

    expect(load).toHaveBeenCalledTimes(2)
    expect(sources[0]!.closed).toBe(true)
    expect(sources).toHaveLength(2)
  })

  it('marks a silent stream stale and reconnects', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    await vi.advanceTimersByTimeAsync(13_000)

    expect(sources[0]!.closed).toBe(true)
    expect(load).toHaveBeenCalledTimes(2)
    expect(sources.length).toBeGreaterThanOrEqual(2)
  })

  it('keeps a stream that reports its state alive', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    for (let second = 0; second < 30; second += 5) {
      await vi.advanceTimersByTimeAsync(5000)
      sources[0]!.emit('state', { kind: 'State', segmentId: null, frames: null, state: 'Stale' })
    }

    expect(sources).toHaveLength(1)
    expect(view?.state).toBe('Stale')
  })

  it('retries when the browser gives up on the stream', async () => {
    start()
    await vi.advanceTimersByTimeAsync(0)

    sources[0]!.readyState = 2
    sources[0]!.emit('error')
    expect(view?.state).toBe('Stale')
    await vi.advanceTimersByTimeAsync(2000)

    expect(load).toHaveBeenCalledTimes(2)
  })

  it('shows the problem and retries until the dashboard answers', async () => {
    results = [{ data: null, problem: 'network' }]
    start()
    await vi.advanceTimersByTimeAsync(0)
    expect(view?.problem).toBe('network')

    await vi.advanceTimersByTimeAsync(2000)

    expect(view?.problem).toBeNull()
    expect(view?.buffer).not.toBeNull()
  })

  it('never opens a stream after it stopped', async () => {
    const stream = start()
    stream.stop()
    await vi.advanceTimersByTimeAsync(0)

    expect(sources).toHaveLength(0)
  })
})
