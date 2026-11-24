import { api, type ApiClient } from '~/api/client'
import type { HealthResponse } from '~/api/types'

/** Reads `/api/health`; `null` when the API errors or cannot be reached. */
export async function loadHealth(client: ApiClient = api): Promise<HealthResponse | null> {
  try {
    const { data } = await client.GET('/api/health')
    return data ?? null
  }
  catch {
    return null
  }
}

/** Version of the running dashboard and the state of the database. */
export function useHealth() {
  const { data: health, refresh } = useAsyncData('health', () => loadHealth(), { default: () => null })
  return {
    version: computed(() => health.value?.version ?? null),
    storage: computed(() => health.value?.storage ?? null),
    refresh,
  }
}
