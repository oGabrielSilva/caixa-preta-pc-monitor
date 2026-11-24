import { describe, expect, it } from 'vitest'
import { eventKey } from '~/utils/event-keys'
import { loadLocale, localeKeys } from '~~/test/support/locale-keys'

describe('eventKey', () => {
  it('names known windows events', () => {
    expect(eventKey('Microsoft-Windows-Kernel-Power', 41)).toBe('events.kernel_power_41')
    expect(eventKey('EventLog', 6008)).toBe('events.eventlog_6008')
    expect(eventKey('Microsoft-Windows-WHEA-Logger', 18)).toBe('events.whea')
  })

  it('falls back for unknown events', () => {
    expect(eventKey('Other', 1)).toBe('events.unknown')
  })

  it('has a message for every key it returns', () => {
    const keys = localeKeys(loadLocale('pt-BR'))
    const samples: [string, number][] = [['Microsoft-Windows-Kernel-Power', 41], ['Microsoft-Windows-Kernel-Power', 42],
      ['Microsoft-Windows-Kernel-Power', 107], ['Microsoft-Windows-Kernel-Power', 109], ['EventLog', 6005], ['EventLog', 6006],
      ['EventLog', 6008], ['User32', 1074], ['User32', 1076], ['Microsoft-Windows-WER-SystemErrorReporting', 1001],
      ['Microsoft-Windows-WHEA-Logger', 1], ['Other', 1]]
    for (const [provider, id] of samples) {
      expect(keys).toContain(eventKey(provider, id))
    }
  })
})
