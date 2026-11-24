import { describe, expect, it } from 'vitest'
import { parseRange } from '~/utils/history-selection'

describe('parseRange', () => {
  it('accepts the known ranges and falls back to a day', () => {
    expect(parseRange('Week')).toBe('Week')
    expect(parseRange('Year')).toBe('Day')
    expect(parseRange(undefined)).toBe('Day')
    expect(parseRange(['Hour'])).toBe('Hour')
  })
})
