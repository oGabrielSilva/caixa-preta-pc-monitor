import { describe, expect, it } from 'vitest'
import { exportFileName } from '~/utils/export/file-name'

describe('exportFileName', () => {
  it('names files with the product, the kind and the local time', () => {
    expect(exportFileName('quedas', 'xlsx', new Date(2026, 10, 26, 0, 12))).toBe('caixa-preta-quedas-2026-11-26-0012.xlsx')
  })
})
