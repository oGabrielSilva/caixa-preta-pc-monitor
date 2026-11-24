import type { LiveEvent, LiveResponse, LiveState } from '~/api/types'
import type { ApiResult } from '~/composables/api-result'
import { appendFrames, createLiveBuffer, type LiveBuffer } from '~/utils/live-buffer'

/** What the stream needs from `EventSource`, so tests can fake it. */
export interface EventSourceLike {
  readonly readyState: number
  addEventListener(type: string, listener: (event: MessageEvent<string>) => void): void
  close(): void
}

export interface LiveStreamView {
  readonly buffer: LiveBuffer | null
  readonly snapshot: LiveResponse | null
  readonly state: LiveState
  readonly problem: string | null
}

export interface LiveStreamOptions {
  load(): Promise<ApiResult<LiveResponse>>
  open(url: string): EventSourceLike
  onChange(view: LiveStreamView): void
  /** The server reports its state every 5 s; longer silence means the stream is dead. */
  silenceMs?: number
  /** First retry delay; doubles up to 30 s while the dashboard does not answer. */
  retryMs?: number
}

const closed = 2
const maxRetryMs = 30_000

/**
 * The live connection: a snapshot (`load`), then server-sent events. A new segment reloads the snapshot. A stream that
 * goes silent or that the browser closes for good (for example after a 503) is dropped, the state turns `Stale`, and
 * the snapshot is reloaded with a growing delay until the dashboard answers again. Nothing reopens after `stop`.
 */
export function createLiveStream(options: LiveStreamOptions) {
  const silenceMs = options.silenceMs ?? 12_000
  const firstRetryMs = options.retryMs ?? 2_000
  let view: LiveStreamView = { buffer: null, snapshot: null, state: 'NoData', problem: null }
  let source: EventSourceLike | null = null
  let lastEventAt = 0
  let retryMs = firstRetryMs
  let retryTimer: ReturnType<typeof setTimeout> | undefined
  let watchdog: ReturnType<typeof setInterval> | undefined
  let stopped = false

  const emit = (patch: Partial<LiveStreamView>) => {
    view = { ...view, ...patch }
    options.onChange(view)
  }
  const parse = (event: MessageEvent<string>) => JSON.parse(event.data) as LiveEvent

  function drop() {
    source?.close()
    source = null
  }

  function retryLater() {
    clearTimeout(retryTimer)
    retryTimer = setTimeout(() => void reload(), retryMs)
    retryMs = Math.min(retryMs * 2, maxRetryMs)
  }

  function connect(buffer: LiveBuffer) {
    drop()
    const query = new URLSearchParams()
    if (buffer.segmentId !== null) {
      query.set('segmentId', String(buffer.segmentId))
    }
    const last = buffer.frames.at(-1)?.t
    if (last !== undefined) {
      query.set('after', String(last))
    }
    const opened = options.open(`/api/live/stream?${query}`)
    source = opened
    lastEventAt = Date.now()
    opened.addEventListener('frames', (message) => {
      lastEventAt = Date.now()
      const event = parse(message)
      const next = view.buffer && event.frames ? appendFrames(view.buffer, event.segmentId, event.frames) : view.buffer
      if (next !== view.buffer) {
        emit({ buffer: next, state: 'Recording' })
      }
    })
    opened.addEventListener('segment', () => {
      lastEventAt = Date.now()
      void reload()
    })
    opened.addEventListener('state', (message) => {
      lastEventAt = Date.now()
      const state = parse(message).state
      if (state) {
        emit({ state })
      }
    })
    opened.addEventListener('error', () => {
      if (opened === source && opened.readyState === closed) {
        drop()
        emit({ state: 'Stale' })
        retryLater()
      }
    })
  }

  async function reload() {
    clearTimeout(retryTimer)
    const result = await options.load()
    if (stopped) {
      return
    }
    if (!result.data) {
      drop()
      emit({ problem: result.problem, state: 'Stale' })
      retryLater()
      return
    }
    retryMs = firstRetryMs
    const buffer = createLiveBuffer(result.data)
    emit({ snapshot: result.data, buffer, state: result.data.state, problem: null })
    connect(buffer)
  }

  function checkSilence() {
    if (source && Date.now() - lastEventAt > silenceMs) {
      drop()
      emit({ state: 'Stale' })
      void reload()
    }
  }

  return {
    start() {
      watchdog = setInterval(checkSilence, 1000)
      void reload()
    },
    stop() {
      stopped = true
      drop()
      clearTimeout(retryTimer)
      clearInterval(watchdog)
    },
  }
}
