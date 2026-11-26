// The release history shown by the landing page, read from the GitHub Releases of the repository at build time.
import { REPO, RELEASES_PAGE } from './site'

export interface ReleaseAsset {
  name: string
  url: string
  size: number
}

export interface Release {
  /** Without the leading "v": `1.0.0`. */
  version: string
  tag: string
  /** ISO 8601. */
  publishedAt: string
  prerelease: boolean
  /** Release notes in GitHub-flavoured markdown, as written (untrusted text). */
  notes: string
  htmlUrl: string
  installer: ReleaseAsset | null
  checksum: ReleaseAsset | null
}

/** The installer that release.yml attaches: `CaixaPreta-Setup-<version>.exe`. */
const INSTALLER = /^CaixaPreta-Setup-.+\.exe$/

interface GitHubAsset {
  name: string
  size: number
  browser_download_url: string
}

interface GitHubRelease {
  tag_name: string
  draft: boolean
  prerelease: boolean
  published_at: string
  html_url: string
  body?: string | null
  assets?: GitHubAsset[]
}

function isPublishedRelease(value: unknown): value is GitHubRelease {
  const release = value as Partial<GitHubRelease> | null
  return typeof release === 'object' && release !== null
    && typeof release.tag_name === 'string' && typeof release.published_at === 'string' && release.draft !== true
}

function asset(assets: GitHubAsset[], name: (candidate: string) => boolean): ReleaseAsset | null {
  const found = assets.find(a => name(a.name))
  return found ? { name: found.name, url: found.browser_download_url, size: found.size } : null
}

/** Published releases (drafts left out), newest first. Anything that is not a release object is ignored. */
export function mapReleases(raw: unknown[]): Release[] {
  return raw.filter(isPublishedRelease)
    .map((release) => {
      const assets = release.assets ?? []
      const installer = asset(assets, name => INSTALLER.test(name))
      return {
        version: release.tag_name.replace(/^v/, ''),
        tag: release.tag_name,
        publishedAt: release.published_at,
        prerelease: release.prerelease === true,
        notes: release.body ?? '',
        htmlUrl: release.html_url,
        installer,
        checksum: installer ? asset(assets, name => name === `${installer.name}.sha256`) : null,
      }
    })
    .sort((a, b) => b.publishedAt.localeCompare(a.publishedAt))
}

/** The release the download button offers: the newest one that is not a prerelease and has an installer. */
export function latestStable(releases: Release[]): Release | null {
  return releases.find(release => !release.prerelease && release.installer !== null) ?? null
}

/** Direct link to the latest stable installer, or the releases page while there is none. */
export function downloadHref(releases: Release[]): string {
  return latestStable(releases)?.installer?.url ?? RELEASES_PAGE
}

/**
 * Reads the releases from the GitHub REST API. Never throws: a private or missing repository, no network or the rate
 * limit all give an empty list, so the site still builds and shows "first release coming soon".
 */
export async function fetchReleases(fetcher: typeof fetch = fetch, token?: string): Promise<Release[]> {
  const headers: Record<string, string> = { 'Accept': 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' }
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  try {
    const response = await fetcher(`https://api.github.com/repos/${REPO}/releases?per_page=100`, { headers })
    if (!response.ok) {
      return []
    }

    const body: unknown = await response.json()
    return Array.isArray(body) ? mapReleases(body) : []
  }
  catch {
    return []
  }
}
