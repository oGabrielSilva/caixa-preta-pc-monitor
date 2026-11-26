import { describe, expect, it } from 'vitest'
import raw from '../fixtures/github-releases.json'
import { downloadHref, fetchReleases, latestStable, mapReleases } from '~~/shared/utils/releases'
import { RELEASES_PAGE } from '~~/shared/utils/site'

const respond = (status: number, body: unknown): typeof fetch =>
  (async () => new Response(JSON.stringify(body), { status })) as typeof fetch

describe('mapReleases', () => {
  const releases = mapReleases(raw)

  it('leaves drafts out and lists the newest first', () => {
    expect(releases.map(r => r.version)).toEqual(['1.2.0-beta.1', '1.1.1', '1.1.0', '1.0.0'])
  })

  it('finds the installer and its checksum among the assets', () => {
    const first = releases.find(r => r.version === '1.0.0')!
    expect(first.installer).toEqual({
      name: 'CaixaPreta-Setup-1.0.0.exe',
      size: 58720256,
      url: 'https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases/download/v1.0.0/CaixaPreta-Setup-1.0.0.exe',
    })
    expect(first.checksum?.name).toBe('CaixaPreta-Setup-1.0.0.exe.sha256')
    expect(releases.find(r => r.version === '1.1.1')!.installer).toBeNull()
  })

  it('keeps the notes as text (rendering is the page\'s job)', () => {
    expect(releases.find(r => r.version === '1.0.0')!.notes).toContain('<script>alert(1)</script>')
  })

  it('ignores anything that is not a release object', () => {
    expect(mapReleases([null, 'x', { tag_name: 1 }])).toEqual([])
  })
})

describe('latestStable', () => {
  it('is the newest release that is not a prerelease and has an installer', () => {
    expect(latestStable(mapReleases(raw))?.version).toBe('1.1.0')
  })

  it('is null before the first release', () => {
    expect(latestStable([])).toBeNull()
  })
})

describe('downloadHref', () => {
  it('points at the latest stable installer', () => {
    expect(downloadHref(mapReleases(raw))).toBe('https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases/download/v1.1.0/CaixaPreta-Setup-1.1.0.exe')
  })

  it('falls back to the releases page while nothing is published', () => {
    expect(downloadHref([])).toBe(RELEASES_PAGE)
  })
})

describe('fetchReleases', () => {
  it('maps the GitHub answer', async () => {
    expect(await fetchReleases(respond(200, raw))).toHaveLength(4)
  })

  it('gives an empty list when the repository is private or missing (404)', async () => {
    expect(await fetchReleases(respond(404, { message: 'Not Found' }))).toEqual([])
  })

  it('gives an empty list without network', async () => {
    const offline = (async () => {
      throw new TypeError('fetch failed')
    }) as typeof fetch
    expect(await fetchReleases(offline)).toEqual([])
  })

  it('asks the GitHub REST API, with the token when there is one', async () => {
    let seen: { url: string, headers: Headers } | undefined
    const spy = (async (url: string, init?: RequestInit) => {
      seen = { url, headers: new Headers(init?.headers) }
      return new Response('[]')
    }) as typeof fetch
    await fetchReleases(spy, 'secret-token')
    expect(seen?.url).toBe('https://api.github.com/repos/oGabrielSilva/caixa-preta-pc-monitor/releases?per_page=100')
    expect(seen?.headers.get('Accept')).toBe('application/vnd.github+json')
    expect(seen?.headers.get('Authorization')).toBe('Bearer secret-token')
  })
})
