import { afterEach, describe, expect, it } from 'vitest'
import { reportHostSize } from '~/composables/useHostWindow'
import { fakeWebViewHost, removeWebViewHost } from '~~/test/support/webview-host'

describe('reportHostSize', () => {
  afterEach(removeWebViewHost)

  it('tells the tray app the panel size in whole CSS pixels, rounded up so nothing is cut', () => {
    const messages = fakeWebViewHost()

    reportHostSize(287.2, 40)

    expect(messages).toEqual([{ type: 'size', width: 288, height: 40 }])
  })

  it('does nothing in a plain browser', () => {
    expect(() => reportHostSize(100, 40)).not.toThrow()
  })
})
