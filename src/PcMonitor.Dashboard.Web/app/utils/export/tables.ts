import type { Finding, HistoryResponse, IncidentDetail, IncidentSummary } from '~/api/types'
import { findingKey } from '~/utils/finding-keys'
import { findingParams, type Translate } from '~/utils/findings'
import { failureTime } from '~/utils/incident-stats'
import { formatDateTime } from '~/utils/format'
import type { ExportSheet, ExportTable } from '~/utils/export/table'

/** The text of a finding, as the dashboard shows it. */
export function findingText(finding: Finding, t: Translate, locale: string): string {
  const key = findingKey(finding.code)
  return key === 'findings.unknown' ? t(key, { code: finding.code }) : t(key, findingParams(finding.parameters, locale, t))
}

function signals(incident: IncidentSummary, t: Translate): string {
  return [
    incident.hasTelemetry ? t('incidents.badges.telemetry') : null,
    incident.sleepState !== 0 ? t('incidents.badges.sleep') : null,
    incident.observations.includes('boot.early_failure') ? t('incidents.badges.earlyBoot') : null,
  ].filter(Boolean).join(', ')
}

/** One row per crash: when, how precise, likely cause, confidence, signals. */
export function incidentTable(incidents: readonly IncidentSummary[], t: Translate, locale: string): ExportTable {
  return {
    headers: [t('incidents.table.when'), t('incidents.table.precision'), t('incidents.table.cause'), t('export.confidence'), t('incidents.table.signals')],
    rows: incidents.map(incident => [
      formatDateTime(failureTime(incident).getTime(), locale),
      t(`incidents.precision.${incident.timeSource}`),
      incident.confidence === null
        ? t('incidents.notDiagnosed')
        : incident.topHypothesis ? t(`hypotheses.${incident.topHypothesis}`) : t('incidents.noConclusion'),
      incident.confidence === null ? null : t(`confidence.${incident.confidence}`),
      signals(incident, t),
    ]),
  }
}

/** One row per finding of every diagnosed crash, with the translated text. */
export function diagnosisTable(details: readonly IncidentDetail[], t: Translate, locale: string): ExportTable {
  return {
    headers: [t('incidents.table.when'), t('export.kind'), t('export.text'), t('export.hypotheses'), t('export.direction'), t('export.strength')],
    rows: details.flatMap(detail => [...(detail.diagnosis?.findings ?? []), ...(detail.diagnosis?.limitations ?? [])].map(finding => [
      formatDateTime(failureTime(detail.summary).getTime(), locale),
      t(`export.kinds.${finding.kind}`),
      findingText(finding, t, locale),
      finding.hypothesis ? t(`hypotheses.${finding.hypothesis}`) : null,
      finding.kind === 'Evidence' ? t(finding.supports ? 'export.for' : 'export.against') : null,
      finding.kind === 'Evidence' ? t(`strength.${finding.strength}`) : null,
    ])),
  }
}

/** One row per history bucket: start, minimum, mean, maximum, unit. */
export function historyTable(history: HistoryResponse | null, t: Translate, locale: string): ExportTable {
  return {
    headers: [t('export.start'), t('history.series.min'), t('history.series.avg'), t('history.series.max'), t('export.unit')],
    rows: (history?.buckets ?? []).map(bucket => [formatDateTime(bucket.t, locale), bucket.min, bucket.avg, bucket.max, history!.sensor.unit]),
  }
}

/** The Excel workbook: crashes, diagnosis findings and (from the history page) the readings of the chosen sensor. */
export function workbookSheets(
  data: { incidents: readonly IncidentSummary[], details: readonly IncidentDetail[], history: HistoryResponse | null },
  t: Translate,
  locale: string,
): ExportSheet[] {
  return [
    { name: t('export.sheets.incidents'), ...incidentTable(data.incidents, t, locale) },
    { name: t('export.sheets.diagnosis'), ...diagnosisTable(data.details, t, locale) },
    { name: t('export.sheets.readings'), ...historyTable(data.history, t, locale) },
  ]
}
