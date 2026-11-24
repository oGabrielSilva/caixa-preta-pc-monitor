import { api, type ApiClient } from '~/api/client'
import type { IncidentDetail } from '~/api/types'
import { toApiResult, type ApiResult } from '~/composables/api-result'

export function loadIncident(id: number, client: ApiClient = api): Promise<ApiResult<IncidentDetail>> {
  return toApiResult(() => client.GET('/api/incidents/{id}', { params: { path: { id } } }))
}

/** One incident with its diagnosis, events and telemetry. */
export function useIncident(id: MaybeRefOrGetter<number>) {
  const { data, status } = useAsyncData(
    () => `incident-${toValue(id)}`,
    () => loadIncident(toValue(id)),
    { default: () => ({ data: null, problem: null }), watch: [() => toValue(id)] },
  )
  return {
    incident: computed(() => data.value.data),
    problem: computed(() => data.value.problem),
    pending: computed(() => status.value === 'pending'),
  }
}
