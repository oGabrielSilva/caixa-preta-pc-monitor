import createClient from 'openapi-fetch'
import type { ApiClient } from '~/api/client'

/** Builds a typed API client whose requests are answered by `handler` instead of the network. */
export function fakeApiClient(handler: (request: Request) => Response | Promise<Response>): ApiClient {
  return createClient({ baseUrl: 'http://dashboard.test', fetch: async request => handler(request) })
}

export const json = (body: unknown, status = 200): Response =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
