/** What a loader returns: the data, or why there is none (a storage problem type from a 503, or `network`). */
export interface ApiResult<T> {
  readonly data: T | null
  readonly problem: string | null
}

interface FetchOutcome<T> {
  readonly data?: T
  readonly error?: unknown
  readonly response: Response
}

/** Runs one openapi-fetch call and turns any failure into a `problem`. */
export async function toApiResult<T>(call: () => Promise<FetchOutcome<T>>): Promise<ApiResult<T>> {
  try {
    const { data, error, response } = await call()
    if (data !== undefined && response.ok) {
      return { data, problem: null }
    }
    const type = typeof error === 'object' && error !== null && 'type' in error ? String(error.type) : null
    return { data: null, problem: type ?? `http.${response.status}` }
  }
  catch {
    return { data: null, problem: 'network' }
  }
}
