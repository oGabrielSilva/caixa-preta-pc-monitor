import { releases } from '#releases'
import { llmsTxt } from '../../shared/utils/llms'
import { downloadHref } from '../../shared/utils/releases'

// Prerendered into a static llms.txt with the download link of this build.
export default defineEventHandler((event) => {
  setHeader(event, 'Content-Type', 'text/plain; charset=utf-8')
  return llmsTxt(downloadHref(releases))
})
