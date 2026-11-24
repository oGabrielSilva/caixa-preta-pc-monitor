import { describe, expect, it } from 'vitest'
import { loadIncidents } from '~/composables/useIncidents'
import { loadHealth } from '~/composables/useHealth'
import { fakeApiClient, json } from '~~/test/support/api-client'
import { incident } from '~~/test/support/fixtures'

describe('api loaders', () => {
  it('returns the incidents', async () => {
    const client = fakeApiClient(() => json([incident({ id: 3 })]))

    expect(await loadIncidents(client)).toEqual({ data: [incident({ id: 3 })], problem: null })
  })

  it('reports the storage problem of a 503', async () => {
    const client = fakeApiClient(() => json({ type: 'storage.missing', status: 503 }, 503))

    expect(await loadIncidents(client)).toEqual({ data: null, problem: 'storage.missing' })
  })

  it('reports an unreachable api', async () => {
    expect(await loadIncidents(fakeApiClient(() => Promise.reject(new TypeError('fetch failed')))))
      .toEqual({ data: null, problem: 'network' })
  })

  it('reads version and storage from health', async () => {
    const client = fakeApiClient(() => json({ status: 'ok', version: '0.1.0', storage: 'Missing' }))

    expect(await loadHealth(client)).toEqual({ status: 'ok', version: '0.1.0', storage: 'Missing' })
  })
})
