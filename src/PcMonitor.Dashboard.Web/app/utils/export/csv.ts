import { noReading } from '~/utils/format'
import type { ExportTable } from '~/utils/export/table'

/**
 * A CSV file the Excel of the user's language opens directly: UTF-8 with a BOM, `;` where the decimal separator is a
 * comma (pt-BR), numbers in the user's format, `—` for a missing value, CRLF line breaks.
 */
export function toCsv(table: ExportTable, locale: string): string {
  const separator = (1.5).toLocaleString(locale).includes(',') ? ';' : ','
  const cell = (value: string | number | null) => {
    const text = value === null ? noReading : typeof value === 'number' ? value.toLocaleString(locale, { maximumFractionDigits: 3, useGrouping: false }) : value
    return text.includes(separator) || /["\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
  }
  const lines = [table.headers, ...table.rows].map(row => row.map(cell).join(separator))
  return `\uFEFF${lines.join('\r\n')}`
}
