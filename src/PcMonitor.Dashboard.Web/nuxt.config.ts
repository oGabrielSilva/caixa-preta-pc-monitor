// https://nuxt.com/docs/api/configuration/nuxt-config
// Explicit import (instead of the auto-import) so tests can load this file outside Nuxt.
import { defineNuxtConfig } from 'nuxt/config'
import { appLocales, defaultLocale } from './i18n/app-locales'

export default defineNuxtConfig({
  modules: ['@nuxt/ui', '@nuxtjs/i18n', '@nuxt/eslint', '@nuxt/test-utils/module'],

  // Static SPA served by PcMonitor.Dashboard (ASP.NET Core); no Node runtime on the user's machine.
  ssr: false,

  devtools: { enabled: true },

  css: ['~/assets/css/main.css'],

  colorMode: {
    preference: 'dark',
  },

  compatibilityDate: '2026-11-24',

  vite: {
    server: {
      // `nuxt dev` forwards API calls to a locally running dashboard host.
      proxy: { '/api': 'http://127.0.0.1:5280' },
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
    strategy: 'no_prefix',
    locales: [...appLocales],
  },

  // Icons are bundled at build time: the dashboard never calls a CDN (nothing leaves the machine)
  // and keeps working offline.
  icon: {
    provider: 'none',
    fallbackToApi: false,
    clientBundle: {
      scan: true,
    },
  },
})
