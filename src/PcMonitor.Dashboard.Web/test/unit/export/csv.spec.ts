import { describe, expect, it } from 'vitest'
import { incidentTable } from '~/utils/export/tables'
import { toCsv } from '~/utils/export/csv'
import { incident } from '~~/test/support/fixtures'

const t = (key: string) => key
const bom = '﻿'

describe('toCsv', () => {
  it('writes a BOM, ; separators and local numbers in pt-BR', () => {
    const csv = toCsv({ headers: ['Sensor', 'Valor'], rows: [['+12 V', 11.28]] }, 'pt-BR')

    expect(csv.startsWith(bom)).toBe(true)
    expect(csv.slice(1).split('\r\n')).toEqual(['Sensor;Valor', '+12 V;11,28'])
  })

  it('uses commas in English', () => {
    expect(toCsv({ headers: ['a', 'b'], rows: [[1.5, 'x']] }, 'en').slice(1)).toBe('a,b\r\n1.5,x')
  })

  it('quotes values that contain the separator, quotes or line breaks', () => {
    const csv = toCsv({ headers: ['texto'], rows: [['a;b'], ['diz "oi"'], ['linha\nnova']] }, 'pt-BR')

    expect(csv.slice(1).split('\r\n').slice(1)).toEqual(['"a;b"', '"diz ""oi"""', '"linha\nnova"'])
  })

  it('writes — for a missing value', () => {
    expect(toCsv({ headers: ['v'], rows: [[null]] }, 'pt-BR').slice(1)).toBe('v\r\n—')
  })
})

describe('incidentTable', () => {
  it('leaves the diagnosis columns as — for an undiagnosed incident', () => {
    const table = incidentTable([incident({ id: 3, confidence: null, topHypothesis: null })], t, 'pt-BR')

    expect(table.rows[0]!.slice(2, 4)).toEqual(['incidents.notDiagnosed', null])
  })
})
