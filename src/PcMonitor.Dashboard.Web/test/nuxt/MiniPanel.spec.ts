// @vitest-environment nuxt
import { afterEach, describe, expect, it } from 'vitest'
import MiniPanel from '~/components/mini/MiniPanel.vue'
import { mountPtBR } from '~~/test/support/nuxt'
import { fakeWebViewHost, removeWebViewHost } from '~~/test/support/webview-host'

const readings = [
  { role: 'CpuTemperature', labelKey: 'mini.cpu', value: 51.4, digits: 1, status: 'success' },
  { role: 'Rail12V', labelKey: 'roles.Rail12V', value: 11.28, digits: 2, status: 'error' },
  { role: 'Rail3V3', labelKey: 'roles.Rail3V3', value: null, digits: 2, status: 'neutral' },
] as const

function mountPanel(unseenIncidentId: number | null = null) {
  return mountPtBR(MiniPanel, { props: { readings: [...readings], dot: 'success', unseenIncidentId } })
}

describe('MiniPanel', () => {
  afterEach(removeWebViewHost)

  it('shows the readings in the user locale and paints an abnormal one red', async () => {
    const panel = await mountPanel()

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
    const messages = fakeWebViewHost()
    const panel = await mountPanel()

    await panel.get('[data-drag]').trigger('mousedown')
    await panel.get('[data-drag]').trigger('click')

    expect(messages).toEqual([{ type: 'drag' }])
    expect(panel.emitted('open')).toBeUndefined()
  })

  it('asks the tray app to resize the window from the corner grip, without opening the dashboard', async () => {
    const messages = fakeWebViewHost()
    const panel = await mountPanel()

    await panel.get('[data-resize]').trigger('mousedown')
    await panel.get('[data-resize]').trigger('click')

    expect(messages).toEqual([{ type: 'resize' }])
    expect(panel.emitted('open')).toBeUndefined()
  })

  it('takes the width of its content, so the window can fit it exactly', async () => {
    const panel = await mountPanel(17)

    expect(panel.classes()).toContain('w-max')
    expect(panel.classes()).not.toContain('w-full')
  })

  it('asks the tray app to grow or shrink with Ctrl + mouse wheel, and ignores the wheel alone', async () => {
    const messages = fakeWebViewHost()
    const panel = await mountPanel()

    await panel.trigger('wheel', { deltaY: -100, ctrlKey: true })
    await panel.trigger('wheel', { deltaY: 100, ctrlKey: true })
    await panel.trigger('wheel', { deltaY: 100 })

    expect(messages).toEqual([{ type: 'scale', step: 1 }, { type: 'scale', step: -1 }])
  })
})
