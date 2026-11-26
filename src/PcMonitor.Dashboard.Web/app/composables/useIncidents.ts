import { api, type ApiClient } from '~/api/client'
import type { IncidentSummary } from '~/api/types'
import { toApiResult, type ApiResult } from '~/composables/api-result'

export function loadIncidents(client: ApiClient = api): Promise<ApiResult<IncidentSummary[]>> {
  return toApiResult(() => client.GET('/api/incidents'))
}

/** Every incident, newest first. */
export function useIncidents() {
  const { data, status, refresh } = useAsyncData('incidents', () => loadIncidents(), { default: () => ({ data: null, problem: null }) })
  return {
    refresh,
    incidents: computed(() => data.value.data ?? []),
    problem: computed(() => data.value.problem),
    pending: computed(() => status.value === 'pending'),
  }
}
