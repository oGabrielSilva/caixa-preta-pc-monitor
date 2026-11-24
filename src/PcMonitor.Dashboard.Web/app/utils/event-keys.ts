const known: Readonly<Record<string, string>> = {
  'Microsoft-Windows-Kernel-Power:41': 'kernel_power_41',
  'Microsoft-Windows-Kernel-Power:42': 'kernel_power_42',
  'Microsoft-Windows-Kernel-Power:107': 'kernel_power_107',
  'Microsoft-Windows-Kernel-Power:109': 'kernel_power_109',
  'EventLog:6005': 'eventlog_6005',
  'EventLog:6006': 'eventlog_6006',
  'EventLog:6008': 'eventlog_6008',
  'User32:1074': 'user32_1074',
  'User32:1076': 'user32_1076',
  'Microsoft-Windows-WER-SystemErrorReporting:1001': 'wer_1001',
}

/** The i18n key that names a Windows event (the events the collector records around an incident). */
export function eventKey(provider: string, eventId: number): string {
  if (provider === 'Microsoft-Windows-WHEA-Logger') {
    return 'events.whea'
  }
  const key = known[`${provider}:${eventId}`]
  return key ? `events.${key}` : 'events.unknown'
}
