import type { Content, TDocumentDefinitions } from 'pdfmake/interfaces'
import type { IncidentDetail, IncidentSummary } from '~/api/types'
import type { Translate } from '~/utils/findings'
import { failureTime } from '~/utils/incident-stats'
import { formatFullDateTime } from '~/utils/format'
import { findingText, incidentTable } from '~/utils/export/tables'
import { telemetrySvg } from '~/utils/export/telemetry-svg'

const styles: TDocumentDefinitions['styles'] = {
  title: { fontSize: 16, bold: true, margin: [0, 0, 0, 4] },
  heading: { fontSize: 12, bold: true, margin: [0, 12, 0, 4] },
  muted: { color: '#525252' },
}

const footer = (t: Translate): TDocumentDefinitions['footer'] => (page, pages) =>
  ({ text: `${t('app.title')} · ${page}/${pages}`, alignment: 'center', fontSize: 8, color: '#737373', margin: [0, 10, 0, 0] })

/** The PDF of one crash: likely cause, scores, what weighed in, limits, events and the last 2 minutes of +12 V. */
export function incidentReport(detail: IncidentDetail, t: Translate, locale: string): TDocumentDefinitions {
  const summary = detail.summary
  const diagnosis = detail.diagnosis
  const rail = detail.telemetry?.series.find(s => s.role === 'Rail12V')
  const content: Content[] = [
    { text: t('incident.title', { date: formatFullDateTime(failureTime(summary).getTime(), locale) }), style: 'title' },
    { text: `${t('incident.mostLikely')}: ${summary.topHypothesis ? t(`hypotheses.${summary.topHypothesis}`) : t('incidents.noConclusion')} · ${summary.confidence ? t(`confidence.${summary.confidence}`) : t('incidents.notDiagnosed')}` },
    { text: t(diagnosis && summary.confidence ? `incident.summary.${summary.confidence}` : 'incident.summary.none'), style: 'muted' },
  ]
  if (diagnosis) {
    content.push(
      { text: t('incident.ranking'), style: 'heading' },
      { table: { widths: ['*', 'auto'], body: diagnosis.ranking.map(r => [r.hypothesis ? t(`hypotheses.${r.hypothesis}`) : '', `${r.score > 0 ? '+' : ''}${r.score}`]) }, layout: 'lightHorizontalLines' },
      { text: t('incident.weighed.title'), style: 'heading' },
      { ul: diagnosis.findings.map(f => findingText(f, t, locale)) },
      { text: t('incident.limits.title'), style: 'heading' },
      { ul: diagnosis.limitations.map(f => findingText(f, t, locale)) },
    )
  }
  content.push({ text: t('incident.telemetry.title'), style: 'heading' })
  if (rail) {
    if (!detail.telemetry!.atFailure) {
      content.push({ text: t('incident.telemetry.notAtFailure'), style: 'muted' })
    }
    content.push(
      { text: rail.range ? t('incident.telemetry.rail', { min: rail.range.min.toLocaleString(locale), max: rail.range.max.toLocaleString(locale) }) : t('roles.Rail12V'), style: 'muted' },
      { svg: telemetrySvg(rail.points, rail.range), width: 500 },
    )
  }
  else {
    content.push({ text: t('incident.telemetry.none'), style: 'muted' })
  }
  if (detail.events.length > 0) {
    content.push(
      { text: t('incident.timeline.title'), style: 'heading' },
      { ul: detail.events.map(e => `${formatFullDateTime(e.loggedAt, locale)} · ${e.provider} ${e.eventId}`) },
    )
  }
  return { info: { title: t('app.title') }, content, styles, footer: footer(t), defaultStyle: { fontSize: 10 } }
}

/** The PDF of every crash: the summary table, newest first. */
export function incidentsReport(incidents: readonly IncidentSummary[], t: Translate, locale: string): TDocumentDefinitions {
  const table = incidentTable(incidents, t, locale)
  return {
    info: { title: t('app.title') },
    pageOrientation: 'landscape',
    content: [
      { text: `${t('app.title')} · ${t('nav.incidents')}`, style: 'title' },
      { text: t('export.generatedAt', { date: formatFullDateTime(Date.now(), locale) }), style: 'muted', margin: [0, 0, 0, 8] },
      { table: { headerRows: 1, widths: ['auto', 'auto', '*', 'auto', '*'], body: [[...table.headers], ...table.rows.map(r => r.map(c => c ?? '—'))] }, layout: 'lightHorizontalLines' },
    ],
    styles,
    footer: footer(t),
    defaultStyle: { fontSize: 9 },
  }
}
