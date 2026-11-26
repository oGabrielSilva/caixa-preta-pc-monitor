// Where the landing page lives and what it links to. The single source for absolute URLs (SEO) and outbound links.

/** GitHub Pages host of the project. */
export const SITE_ORIGIN = 'https://ogabrielsilva.github.io'

/** The project path on GitHub Pages; also Nuxt's app.baseURL. */
export const BASE_PATH = '/caixa-preta-pc-monitor/'

/** Absolute URL of the home page, with the trailing slash. */
export const SITE_URL = `${SITE_ORIGIN}${BASE_PATH}`

/** GitHub repository, `owner/name`. */
export const REPO = 'oGabrielSilva/caixa-preta-pc-monitor'

export const REPO_URL = `https://github.com/${REPO}`

/** Where every release lives; the download fallback while no installer is published. */
export const RELEASES_PAGE = `${REPO_URL}/releases`

/** The author as shown publicly: name and GitHub profile only (never an e-mail address). */
export const AUTHOR = { name: 'Gabriel Silva', url: 'https://github.com/oGabrielSilva' } as const
