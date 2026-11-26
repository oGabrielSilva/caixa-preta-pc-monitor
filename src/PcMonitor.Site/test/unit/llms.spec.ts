import { describe, expect, it } from 'vitest'
import { llmsTxt } from '~~/shared/utils/llms'

describe('llmsTxt', () => {
  const text = llmsTxt('https://example.test/CaixaPreta-Setup-1.0.0.exe')

  it('follows the llms.txt shape: a title, a summary quote, then sections', () => {
    expect(text.startsWith('# Caixa-Preta\n\n> ')).toBe(true)
    expect(text).toContain('\n## ')
  })

  it('links the site, the code, the releases and the download', () => {
    expect(text).toContain('https://ogabrielsilva.github.io/caixa-preta-pc-monitor/')
    expect(text).toContain('https://github.com/oGabrielSilva/caixa-preta-pc-monitor')
    expect(text).toContain('https://example.test/CaixaPreta-Setup-1.0.0.exe')
  })

  it('states the facts an assistant should quote correctly', () => {
    for (const fact of ['Windows 11', 'MIT', 'once per second', 'likely cause', 'not code-signed', 'no telemetry']) {
      expect(text, fact).toContain(fact)
    }
  })
})
