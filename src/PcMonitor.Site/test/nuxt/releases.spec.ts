// @vitest-environment nuxt
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import raw from '../fixtures/github-releases.json'
import ReleaseList from '~/components/changelog/ReleaseList.vue'
import Releases from '~/pages/releases.vue'
import { mapReleases } from '~~/shared/utils/releases'
import { RELEASES_PAGE } from '~~/shared/utils/site'

describe('ReleaseList', () => {
  it('lists every published release, newest first, marking the prerelease', async () => {
    const list = await mountSuspended(ReleaseList, { props: { releases: mapReleases(raw) } })
    const versions = list.findAll('[data-release]')
    expect(versions.map(v => v.attributes('data-release'))).toEqual(['1.2.0-beta.1', '1.1.1', '1.1.0', '1.0.0'])
    expect(versions[0]!.text()).toContain('Pré-release')
    expect(versions[2]!.text()).not.toContain('Pré-release')
  })

  it('links the installer and its checksum', async () => {
    const list = await mountSuspended(ReleaseList, { props: { releases: mapReleases(raw) } })
    const hrefs = list.find('[data-release="1.0.0"]').findAll('a').map(a => a.attributes('href'))
    expect(hrefs).toContain('https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases/download/v1.0.0/CaixaPreta-Setup-1.0.0.exe')
    expect(hrefs).toContain('https://github.com/oGabrielSilva/caixa-preta-pc-monitor/releases/download/v1.0.0/CaixaPreta-Setup-1.0.0.exe.sha256')
  })

  it('never turns release notes into HTML', async () => {
    const list = await mountSuspended(ReleaseList, { props: { releases: mapReleases(raw) } })
    expect(list.find('script').exists()).toBe(false)
    expect(list.text()).toContain('<script>alert(1)</script>')
  })

  it('shows the way to GitHub before the first release', async () => {
    const list = await mountSuspended(ReleaseList, { props: { releases: [] } })
    expect(list.find('[data-release]').exists()).toBe(false)
    expect(list.findAll('a').map(a => a.attributes('href'))).toContain(RELEASES_PAGE)
  })
})

describe('releases page', () => {
  it('has one h1 and a breadcrumb in JSON-LD', async () => {
    const page = await mountSuspended(Releases)
    expect(page.findAll('h1')).toHaveLength(1)
    await new Promise(resolve => setTimeout(resolve, 50))
    const types = [...document.head.querySelectorAll('script[type="application/ld+json"]')]
      .map(script => (JSON.parse(script.textContent ?? '{}') as { '@type': string })['@type'])
    expect(types).toContain('BreadcrumbList')
  })
})
