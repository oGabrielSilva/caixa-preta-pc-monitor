// https://nuxt.com/docs/api/configuration/nuxt-config
// Explicit import (instead of the auto-import) so tests can load this file outside Nuxt.
import { defineNuxtConfig } from 'nuxt/config'
import { appLocales, defaultLocale } from './i18n/app-locales'

export default defineNuxtConfig({
  // Theme, fonts and icon settings shared with the landing page (ADR 0021).
  extends: ['../PcMonitor.Brand'],

  modules: ['@nuxt/ui', '@nuxtjs/i18n', '@nuxt/eslint', '@nuxt/test-utils/module'],

  // Static SPA served by PcMonitor.Dashboard (ASP.NET Core); no Node runtime on the user's machine.
  ssr: false,

  devtools: { enabled: true },

  app: {
    head: {
      link: [{ rel: 'icon', type: 'image/svg+xml', href: '/icon.svg' }, { rel: 'icon', href: '/favicon.ico', sizes: '48x48' }],
    },
  },

  css: ['~/assets/css/main.css'],

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
})
