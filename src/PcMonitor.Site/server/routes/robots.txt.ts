import { robotsTxt } from '../../shared/utils/seo'

// Prerendered into a static robots.txt. GitHub Pages serves it from the project path; search engines read the
// sitemap it names.
export default defineEventHandler((event) => {
  setHeader(event, 'Content-Type', 'text/plain; charset=utf-8')
  return robotsTxt()
})
