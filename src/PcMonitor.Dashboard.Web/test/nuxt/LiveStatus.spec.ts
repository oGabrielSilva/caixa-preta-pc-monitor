// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import LiveStatus from '~/components/live/LiveStatus.vue'
import { mountPtBR } from '~~/test/support/nuxt'

describe('LiveStatus', () => {
  it('says how long there have been no readings', async () => {
    const status = await mountPtBR(LiveStatus, { props: { state: 'Stale', session: null, lastFrameAt: Date.now() - 42_000 } })

    expect(status.text()).toContain('Sem leituras há 42 s')
  })
})
