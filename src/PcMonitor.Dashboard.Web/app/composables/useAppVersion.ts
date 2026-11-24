import { api, type ApiClient } from '~/api/client'

/** Fetches the dashboard version; `null` when the API errors or cannot be reached. */
export async function loadAppVersion(client: ApiClient = api): Promise<string | null> {
  try {
    const { data } = await client.GET('/api/health')
    return data?.version ?? null
  }
  catch {
    return null
  }
}

/** Version of the running dashboard, for display. `null` while loading or when unavailable. */
export function useAppVersion() {
  const { data: version } = useAsyncData('app-version', () => loadAppVersion(), { default: () => null })
  return { version }
}
