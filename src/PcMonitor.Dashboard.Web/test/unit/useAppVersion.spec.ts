import { describe, expect, it } from 'vitest'
import { loadAppVersion } from '~/composables/useAppVersion'
import { fakeApiClient, json } from '~~/test/support/api-client'

describe('loadAppVersion', () => {
  it('returns the version reported by /api/health', async () => {
    const client = fakeApiClient(request =>
      new URL(request.url).pathname === '/api/health'
        ? json({ status: 'ok', version: '0.1.0' })
        : json({}, 404))

    expect(await loadAppVersion(client)).toBe('0.1.0')
  })

  it('returns null when the API answers with an error', async () => {
    expect(await loadAppVersion(fakeApiClient(() => json({}, 500)))).toBeNull()
  })

  it('returns null when the API is unreachable', async () => {
    const unreachable = fakeApiClient(() => Promise.reject(new TypeError('fetch failed')))

    expect(await loadAppVersion(unreachable)).toBeNull()
  })
})
