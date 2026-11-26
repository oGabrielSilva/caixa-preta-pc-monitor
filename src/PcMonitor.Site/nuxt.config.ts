// Landing page of Caixa-Preta: rendered to static HTML by `nuxt generate` and published on GitHub Pages (ADR 0021).
// Explicit import (instead of the auto-import) so tests can load this file outside Nuxt.
import { defineNuxtConfig } from 'nuxt/config'
import { defaultLocale, siteLocales } from './i18n/site-locales'
import { BASE_PATH, SITE_URL } from './shared/utils/site'

export default defineNuxtConfig({
  // Theme, fonts and icon settings shared with the dashboard.
  extends: ['../PcMonitor.Brand'],

  modules: ['@nuxt/ui', '@nuxtjs/i18n', '@nuxt/eslint', '@nuxt/test-utils/module'],

  // Every page is rendered to HTML at build time: search engines and AI crawlers read the content without JavaScript.
  ssr: true,

  // Folders group components by area (home/, changelog/, shared/); names stay as written (FlightRecorder, not
  // HomeFlightRecorder).
  components: [{ path: '~/components', pathPrefix: false }],

  devtools: { enabled: true },

  app: {
    baseURL: BASE_PATH,
    head: {
      link: [{ rel: 'icon', type: 'image/svg+xml', href: `${BASE_PATH}icon.svg` }, { rel: 'icon', href: `${BASE_PATH}favicon.ico`, sizes: '48x48' }],
    },
  },

  css: ['~/assets/css/main.css'],

  compatibilityDate: '2026-11-26',

  nitro: {
    prerender: {
      crawlLinks: true,
      // en.html instead of en/index.html: GitHub Pages serves /en from en.html directly, so the canonical URLs
      // (no trailing slash) never go through a redirect.
      autoSubfolderIndex: false,
      routes: ['/', '/en', '/sitemap.xml', '/robots.txt', '/llms.txt'],
    },
  },

  typescript: {
    strict: true,
  },

  eslint: {
    config: { stylistic: true },
  },

  i18n: {
    defaultLocale,
    strategy: 'prefix_except_default',
    // Canonical and hreflang links are built by usePageSeo (headLinks): i18n would repeat the project path.
    baseUrl: SITE_URL.replace(/\/$/, ''),
    locales: [...siteLocales],
    detectBrowserLanguage: false,
    customRoutes: 'config',
    pages: {
      releases: { 'pt-BR': '/versoes', 'en': '/releases' },
    },
  },
})
