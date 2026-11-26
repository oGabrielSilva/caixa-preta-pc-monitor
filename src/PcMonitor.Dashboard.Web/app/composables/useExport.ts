import type { TDocumentDefinitions } from 'pdfmake/interfaces'
import type { HistoryResponse, IncidentDetail, IncidentSummary } from '~/api/types'
import { loadIncident } from '~/composables/useIncident'
import { toCsv } from '~/utils/export/csv'
import { downloadBlob } from '~/utils/export/download'
import { exportFileName } from '~/utils/export/file-name'
import { incidentReport, incidentsReport } from '~/utils/export/incident-report'
import type { ExportSheet, ExportTable } from '~/utils/export/table'
import { historyTable, incidentTable, workbookSheets } from '~/utils/export/tables'

export type ExportFormat = 'csv' | 'xlsx' | 'pdf'

/**
 * Exports what the pages show. The Excel and PDF libraries are loaded only when the user exports (they are large),
 * and the texts are the dashboard's own translations.
 */
export function useExport() {
  const { t, locale } = useI18n()
  const busy = ref(false)

  async function run(task: () => Promise<void>) {
    busy.value = true
    try {
      await task()
    }
    finally {
      busy.value = false
    }
  }

  const csv = (table: ExportTable, kind: string) =>
    downloadBlob(new Blob([toCsv(table, locale.value)], { type: 'text/csv;charset=utf-8' }), exportFileName(kind, 'csv', new Date()))

  async function xlsx(sheets: readonly ExportSheet[], kind: string) {
    const { default: ExcelJS } = await import('exceljs/dist/exceljs.min.js')
    const workbook = new ExcelJS.Workbook()
    for (const sheet of sheets) {
      const worksheet = workbook.addWorksheet(sheet.name.slice(0, 31))
      worksheet.addRow([...sheet.headers]).font = { bold: true }
      sheet.rows.forEach(row => worksheet.addRow(row.map(c => c ?? '—')))
      worksheet.columns.forEach(column => (column.width = 22))
    }
    const buffer = await workbook.xlsx.writeBuffer()
    downloadBlob(new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), exportFileName(kind, 'xlsx', new Date()))
  }

  async function pdf(document: TDocumentDefinitions, kind: string) {
    const [{ default: pdfMake }, { default: fonts }] = await Promise.all([import('pdfmake/build/pdfmake'), import('pdfmake/build/vfs_fonts')])
    pdfMake.addVirtualFileSystem(fonts)
    downloadBlob(await pdfMake.createPdf(document).getBlob(), exportFileName(kind, 'pdf', new Date()))
  }

  async function details(incidents: readonly IncidentSummary[]): Promise<IncidentDetail[]> {
    const results = await Promise.all(incidents.map(i => loadIncident(i.id)))
    return results.flatMap(r => (r.data ? [r.data] : []))
  }

  /** The crash list (Incidents page). */
  function exportIncidents(format: ExportFormat, incidents: readonly IncidentSummary[]) {
    return run(async () => {
      const kind = 'quedas'
      if (format === 'csv') {
        csv(incidentTable(incidents, t, locale.value), kind)
      }
      else if (format === 'xlsx') {
        await xlsx(workbookSheets({ incidents, details: await details(incidents), history: null }, t, locale.value), kind)
      }
      else {
        await pdf(incidentsReport(incidents, t, locale.value), kind)
      }
    })
  }

  /** One crash as a PDF report (detail page). */
  function exportIncident(detail: IncidentDetail) {
    return run(() => pdf(incidentReport(detail, t, locale.value), `queda-${detail.summary.id}`))
  }

  /** The sensor and period shown on the History page, with the crashes and their diagnosis in the workbook. */
  function exportHistory(format: ExportFormat, history: HistoryResponse, incidents: readonly IncidentSummary[]) {
    return run(async () => {
      const kind = 'historico'
      if (format === 'csv') {
        csv(historyTable(history, t, locale.value), kind)
      }
      else {
        await xlsx(workbookSheets({ incidents, details: await details(incidents), history }, t, locale.value), kind)
      }
    })
  }

  return { busy, exportIncidents, exportIncident, exportHistory }
}
