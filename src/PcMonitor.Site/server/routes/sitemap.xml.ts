import { SITE_PAGES, sitemapXml } from '../../shared/utils/seo'

// Prerendered into a static sitemap.xml (nuxt.config: nitro.prerender.routes).
export default defineEventHandler((event) => {
  setHeader(event, 'Content-Type', 'application/xml; charset=utf-8')
  return sitemapXml(SITE_PAGES)
})
