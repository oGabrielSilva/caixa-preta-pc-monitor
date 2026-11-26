import { describe, expect, it } from 'vitest'
import { markSeen, unseenIncident } from '~/utils/seen-incidents'
import { incident } from '~~/test/support/fixtures'

class MemoryStorage {
  readonly values = new Map<string, string>()
  getItem(key: string) { return this.values.get(key) ?? null }
  setItem(key: string, value: string) { this.values.set(key, value) }
}

const deny = (): never => {
  throw new Error('denied')
}
const broken = { getItem: deny, setItem: deny }

describe('seen incidents', () => {
  it('flags an incident newer than the last one seen', () => {
    const storage = new MemoryStorage()
    markSeen(storage, 16)

    expect(unseenIncident(storage, [incident({ id: 17 }), incident({ id: 16 })])?.id).toBe(17)
  })

  it('takes everything as seen the first time, so old crashes do not light up', () => {
    const storage = new MemoryStorage()

    expect(unseenIncident(storage, [incident({ id: 16 })])).toBeNull()
    expect(unseenIncident(storage, [incident({ id: 17 }), incident({ id: 16 })])?.id).toBe(17)
  })

  it('never goes back to an older id', () => {
    const storage = new MemoryStorage()
    markSeen(storage, 17)
    markSeen(storage, 12)

    expect(unseenIncident(storage, [incident({ id: 17 })])).toBeNull()
  })

  it('treats unavailable storage as nothing new', () => {
    expect(() => markSeen(broken, 3)).not.toThrow()
    expect(unseenIncident(broken, [incident({ id: 17 })])).toBeNull()
  })
})
