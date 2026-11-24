import { api, type ApiClient } from '~/api/client'
import type { HistoryRange, HistoryResponse, SensorResponse } from '~/api/types'
import { toApiResult, type ApiResult } from '~/composables/api-result'

export function loadHistorySensors(client: ApiClient = api): Promise<ApiResult<SensorResponse[]>> {
  return toApiResult(() => client.GET('/api/history/sensors'))
}

export function loadHistory(sensor: string, range: HistoryRange, client: ApiClient = api): Promise<ApiResult<HistoryResponse>> {
  return toApiResult(() => client.GET('/api/history', { params: { query: { sensor, range } } }))
}

/** Sensors that have history: those with a role first, then by name. */
export function useHistorySensors() {
  const { data } = useAsyncData('history-sensors', () => loadHistorySensors(), { default: () => ({ data: null, problem: null }) })
  return {
    sensors: computed(() => [...(data.value.data ?? [])].sort((a, b) =>
      Number(a.role === 'None') - Number(b.role === 'None') || a.name.localeCompare(b.name))),
    problem: computed(() => data.value.problem),
  }
}

/** History of one sensor over a period; reloads when either changes. */
export function useHistory(sensor: MaybeRefOrGetter<string | undefined>, range: MaybeRefOrGetter<HistoryRange>) {
  const { data, status } = useAsyncData(
    () => `history-${toValue(sensor)}-${toValue(range)}`,
    async () => {
      const key = toValue(sensor)
      return key ? loadHistory(key, toValue(range)) : { data: null, problem: null }
    },
    { default: () => ({ data: null, problem: null }), watch: [() => toValue(sensor), () => toValue(range)] },
  )
  return {
    history: computed(() => data.value.data),
    problem: computed(() => data.value.problem),
    pending: computed(() => status.value === 'pending'),
  }
}
