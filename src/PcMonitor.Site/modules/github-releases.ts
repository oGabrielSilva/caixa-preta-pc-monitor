// Reads the GitHub Releases once per build and exposes them to the app as `import { releases } from '#releases'`.
// The site is rebuilt after every release (release.yml starts pages.yml), so the data is never older than the build.
import { addTemplate, addTypeTemplate, defineNuxtModule, useLogger } from 'nuxt/kit'
import { fetchReleases } from '../shared/utils/releases'

export default defineNuxtModule({
  meta: { name: 'github-releases' },
  async setup(_options, nuxt) {
    const logger = useLogger('github-releases')
    const releases = await fetchReleases(fetch, process.env.GITHUB_TOKEN)
    if (releases.length === 0) {
      logger.warn('No published release found (private repository, no network or none yet): the site shows "coming soon".')
    }
    else {
      logger.info(`${releases.length} release(s), newest ${releases[0]!.tag}.`)
    }

    const template = addTemplate({
      filename: 'releases.mjs',
      getContents: () => `export const releases = ${JSON.stringify(releases)}\n`,
      write: true,
    })
    nuxt.options.alias['#releases'] = template.dst

    addTypeTemplate({
      filename: 'types/releases.d.ts',
      getContents: () => `declare module '#releases' {\n  export const releases: import('${nuxt.options.rootDir.replace(/\\/g, '/')}/shared/utils/releases').Release[]\n}\n`,
    }, { nuxt: true, nitro: true })
  },
})
