// @vitest-environment nuxt
import { describe, expect, it } from 'vitest'
import MiniPanel from '~/components/mini/MiniPanel.vue'
import { mountPtBR } from '~~/test/support/nuxt'

const readings = [
  { role: 'CpuTemperature', labelKey: 'mini.cpu', value: 51.4, digits: 1, status: 'success' },
  { role: 'Rail12V', labelKey: 'roles.Rail12V', value: 11.28, digits: 2, status: 'error' },
  { role: 'Rail3V3', labelKey: 'roles.Rail3V3', value: null, digits: 2, status: 'neutral' },
] as const

describe('MiniPanel', () => {
  it('shows the readings in the user locale and paints an abnormal one red', async () => {
    const panel = await mountPtBR(MiniPanel, { props: { readings: [...readings], dot: 'success', unseenIncidentId: null } })

    expect(panel.text()).toContain('51,4')
    expect(panel.get('[data-role="Rail12V"] [data-value]').classes()).toContain('text-error')
    expect(panel.get('[data-role="Rail3V3"] [data-value]').text()).toBe('—')
    expect(panel.find('[data-unseen]').exists()).toBe(false)
  })

  it('shows a red mark for an unseen crash', async () => {
    const panel = await mountPtBR(MiniPanel, { props: { readings: [...readings], dot: 'error', unseenIncidentId: 17 } })

    expect(panel.find('[data-unseen]').exists()).toBe(true)
    expect(panel.get('[data-dot]').classes()).toContain('bg-error')
  })
  it('asks the tray app to drag the window from the icon, without opening the dashboard', async () => {
    const messages: unknown[] = []
    Object.assign(globalThis, { chrome: { webview: { postMessage: (message: unknown) => messages.push(message) } } })
    const panel = await mountPtBR(MiniPanel, { props: { readings: [...readings], dot: 'success', unseenIncidentId: null } })

    await panel.get('[data-drag]').trigger('mousedown')
    await panel.get('[data-drag]').trigger('click')

    expect(messages).toEqual([{ type: 'drag' }])
    expect(panel.emitted('open')).toBeUndefined()
    Reflect.deleteProperty(globalThis, 'chrome')
  })
})
