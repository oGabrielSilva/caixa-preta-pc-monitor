import { describe, expect, it } from 'vitest'
import { parseRange, resolveSensor } from '~/utils/history-selection'
import { cpuTemperature, rail12 } from '~~/test/support/fixtures'

describe('parseRange', () => {
  it('accepts the known ranges and falls back to a day', () => {
    expect(parseRange('Week')).toBe('Week')
    expect(parseRange('Year')).toBe('Day')
    expect(parseRange(undefined)).toBe('Day')
    expect(parseRange(['Hour'])).toBe('Hour')
  })
})

describe('resolveSensor', () => {
  it('keeps a recorded sensor and falls back to the first one for an unknown key', () => {
    expect(resolveSensor(rail12.key, [cpuTemperature, rail12])).toBe(rail12.key)
    expect(resolveSensor('/gone', [cpuTemperature, rail12])).toBe(cpuTemperature.key)
    expect(resolveSensor(undefined, [cpuTemperature, rail12])).toBe(cpuTemperature.key)
  })

  it('waits for the sensor list before judging the key', () => {
    expect(resolveSensor('/gone', [])).toBe('/gone')
  })
})
