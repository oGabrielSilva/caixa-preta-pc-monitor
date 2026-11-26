/** Prefixes a file from public/ with the app's base URL (`/caixa-preta-pc-monitor/` on GitHub Pages). */
export function usePublicPath(): (path: string) => string {
  const base = useRuntimeConfig().app.baseURL.replace(/\/$/, '')
  return path => `${base}${path.startsWith('/') ? path : `/${path}`}`
}
