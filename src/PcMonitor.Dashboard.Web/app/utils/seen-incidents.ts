import type { IncidentSummary } from '~/api/types'

/** The part of `localStorage` used here, so tests can pass a fake. */
export interface StorageLike {
  getItem(key: string): string | null
  setItem(key: string, value: string): void
}

const key = 'caixa-preta.seen-incident'

function readSeen(storage: StorageLike): number | null {
  try {
    const value = storage.getItem(key)
    return value === null ? null : Number(value)
  }
  catch {
    return null
  }
}

/** Records that the owner has seen the incidents up to `id`; never goes back. Storage errors are ignored. */
export function markSeen(storage: StorageLike, id: number): void {
  try {
    if ((readSeen(storage) ?? Number.NEGATIVE_INFINITY) < id) {
      storage.setItem(key, String(id))
    }
  }
  catch {
    // Private mode or blocked storage: nothing to remember.
  }
}

/**
 * The newest incident the owner has not seen yet, or `null`. The first time (nothing stored), everything already
 * recorded counts as seen, so old crashes do not light up. Storage that cannot be read means nothing to show.
 */
export function unseenIncident(storage: StorageLike, incidents: readonly IncidentSummary[]): IncidentSummary | null {
  const newest = incidents.reduce<IncidentSummary | null>((top, i) => (top === null || i.id > top.id ? i : top), null)
  if (newest === null) {
    return null
  }
  const seen = readSeen(storage)
  if (seen === null) {
    markSeen(storage, newest.id)
    return null
  }
  return newest.id > seen ? newest : null
}
