import { describe, expect, it } from 'vitest'
import { workbookSheets } from '~/utils/export/tables'

const t = (key: string) => key

describe('workbookSheets', () => {
  it('builds the three sheets with headers even when empty', () => {
    const sheets = workbookSheets({ incidents: [], details: [], history: null }, t, 'pt-BR')

    expect(sheets.map(s => s.name)).toEqual(['export.sheets.incidents', 'export.sheets.diagnosis', 'export.sheets.readings'])
    expect(sheets.every(s => s.headers.length > 0 && s.rows.length === 0)).toBe(true)
  })
})
