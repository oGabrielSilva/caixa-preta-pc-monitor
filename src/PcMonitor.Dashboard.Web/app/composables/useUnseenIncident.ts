import type { IncidentSummary } from '~/api/types'
import { markSeen, unseenIncident, type StorageLike } from '~/utils/seen-incidents'

/** `localStorage`, or a storage that refuses everything when the browser blocks it. */
function storage(): StorageLike {
  try {
    return window.localStorage
  }
  catch {
    return { getItem: () => null, setItem: () => undefined }
  }
}

/** Marks every incident up to the newest as seen (the incidents pages call it when they show the list or one crash). */
export function useMarkIncidentsSeen(incidents: MaybeRefOrGetter<readonly IncidentSummary[]>) {
  watchEffect(() => {
    const newest = Math.max(...toValue(incidents).map(i => i.id))
    if (Number.isFinite(newest)) {
      markSeen(storage(), newest)
    }
  })
}

/** The newest crash the owner has not opened yet; the list is refreshed every 5 minutes. */
export function useUnseenIncident() {
  const { incidents, refresh } = useIncidents()
  const tick = ref(0)
  let timer: ReturnType<typeof setInterval> | undefined
  onMounted(() => {
    timer = setInterval(() => {
      tick.value++
      void refresh()
    }, 5 * 60_000)
  })
  onScopeDispose(() => clearInterval(timer))
  return computed(() => {
    void tick.value
    return unseenIncident(storage(), incidents.value)
  })
}
