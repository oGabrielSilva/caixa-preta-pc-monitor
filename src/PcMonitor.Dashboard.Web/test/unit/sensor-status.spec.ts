import { describe, expect, it } from 'vitest'
import { statusOf } from '~/utils/sensor-status'
import { cpuTemperature, fan, rail12 } from '~~/test/support/fixtures'

describe('statusOf', () => {
  it('classifies rails by the range sent by the api', () => {
    expect([statusOf(12.19, rail12), statusOf(11.3, rail12), statusOf(12.6, rail12)]).toEqual(['success', 'error', 'success'])
  })

  it('classifies the cpu by its limit and critical temperature', () => {
    expect([statusOf(60, cpuTemperature), statusOf(96, cpuTemperature), statusOf(101, cpuTemperature)])
      .toEqual(['success', 'warning', 'error'])
  })

  it('is neutral without a reading or a range', () => {
    expect([statusOf(null, rail12), statusOf(2000, fan)]).toEqual(['neutral', 'neutral'])
  })
})
