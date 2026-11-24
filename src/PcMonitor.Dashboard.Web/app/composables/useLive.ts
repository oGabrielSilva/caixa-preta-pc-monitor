import { api, type ApiClient } from '~/api/client'
import type { LiveEvent, LiveResponse, LiveState } from '~/api/types'
import { toApiResult, type ApiResult } from '~/composables/api-result'
import { appendFrames, createLiveBuffer, type LiveBuffer } from '~/utils/live-buffer'

export function loadLive(client: ApiClient = api): Promise<ApiResult<LiveResponse>> {
  return toApiResult(() => client.GET('/api/live'))
}

/**
 * The live recording: a snapshot of the last ten minutes, then frames pushed by `/api/live/stream` (server-sent
 * events). A new segment or a reconnection reloads the snapshot; the stream closes with the component.
 */
export function useLive() {
  const buffer = shallowRef<LiveBuffer | null>(null)
  const snapshot = shallowRef<LiveResponse | null>(null)
  const state = ref<LiveState>('NoData')
  const problem = ref<string | null>(null)
  let source: EventSource | null = null

  function connect(after: LiveBuffer) {
    source?.close()
    const query = new URLSearchParams()
    if (after.segmentId !== null) {
      query.set('segmentId', String(after.segmentId))
    }
    const last = after.frames.at(-1)?.t
    if (last !== undefined) {
      query.set('after', String(last))
    }
    source = new EventSource(`/api/live/stream?${query}`)
    source.addEventListener('frames', (message) => {
      const event = JSON.parse(message.data) as LiveEvent
      if (buffer.value && event.frames) {
        buffer.value = appendFrames(buffer.value, event.segmentId, event.frames)
        state.value = 'Recording'
      }
    })
    source.addEventListener('segment', () => void reload())
    source.addEventListener('state', (message) => {
      const event = JSON.parse(message.data) as LiveEvent
      if (event.state) {
        state.value = event.state
      }
    })
    // EventSource reconnects by itself; reload so frames missed while disconnected are not lost.
    source.addEventListener('open', () => {
      if (buffer.value && buffer.value !== after) {
        void reload()
      }
    })
  }

  async function reload() {
    const result = await loadLive()
    problem.value = result.problem
    if (result.data) {
      snapshot.value = result.data
      state.value = result.data.state
      buffer.value = createLiveBuffer(result.data)
      connect(buffer.value)
    }
  }

  onMounted(() => void reload())
  onScopeDispose(() => source?.close())

  return {
    buffer,
    state,
    problem,
    session: computed(() => snapshot.value?.session ?? null),
  }
}
