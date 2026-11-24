import { api, type ApiClient } from '~/api/client'
import type { LiveResponse } from '~/api/types'
import { toApiResult, type ApiResult } from '~/composables/api-result'
import { createLiveStream, type LiveStreamView } from '~/composables/live-stream'

export function loadLive(client: ApiClient = api): Promise<ApiResult<LiveResponse>> {
  return toApiResult(() => client.GET('/api/live'))
}

/** The live recording while the component is mounted (see `createLiveStream`). */
export function useLive() {
  const view = shallowRef<LiveStreamView>({ buffer: null, snapshot: null, state: 'NoData', problem: null })
  const stream = createLiveStream({
    load: () => loadLive(),
    open: url => new EventSource(url),
    onChange: (next) => {
      view.value = next
    },
  })

  onMounted(() => stream.start())
  onScopeDispose(() => stream.stop())

  return {
    buffer: computed(() => view.value.buffer),
    state: computed(() => view.value.state),
    problem: computed(() => view.value.problem),
    session: computed(() => view.value.snapshot?.session ?? null),
    lastFrameAt: computed(() => view.value.buffer?.frames.at(-1)?.t ?? null),
  }
}
