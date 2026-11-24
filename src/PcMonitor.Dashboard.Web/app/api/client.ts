import createClient, { type Client } from 'openapi-fetch'
import type { paths } from './schema'

/** Typed client for the dashboard API. Types come from the generated `schema.d.ts`. */
export type ApiClient = Client<paths>

/** The single HTTP client of the app. The SPA is served by the API host, so paths are relative. */
export const api: ApiClient = createClient<paths>({ baseUrl: '' })
