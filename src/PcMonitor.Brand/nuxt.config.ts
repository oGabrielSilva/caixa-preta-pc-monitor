// Shared Nuxt layer: the Caixa-Preta theme used by the dashboard and the landing page (ADR 0015, ADR 0021).
// It has no dependencies of its own (no node_modules): each app that extends it lists the modules (@nuxt/ui).
// A plain object, not defineNuxtConfig(): the layer has no node_modules to import 'nuxt/config' from.
export default {
  $meta: { name: 'brand' },

  colorMode: {
    preference: 'dark',
  },

  // Theme fonts (brand.css) are downloaded at build time and served from /_fonts: no app ever calls a font CDN.
  // Weights match the theme (semibold = 700, bold = 800).
  fonts: {
    defaults: { weights: [400, 500, 700, 800] },
  },

  // Icons are bundled at build time: no app calls the Iconify API, and the dashboard keeps working offline.
  icon: {
    provider: 'none',
    fallbackToApi: false,
    clientBundle: {
      scan: true,
    },
  },
}
