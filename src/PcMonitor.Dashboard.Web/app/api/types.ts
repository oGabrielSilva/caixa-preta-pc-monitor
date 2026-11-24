// Aliases of the generated contract (schema.d.ts). Never declare API shapes by hand.
import type { components } from './schema'

type Schemas = components['schemas']

export type Confidence = NonNullable<Schemas['Confidence']>
export type EventResponse = Schemas['EventResponse']
export type Finding = Schemas['FindingResponse']
export type FindingCode = Schemas['FindingCode']
export type FramePoint = Schemas['FramePoint']
export type HealthResponse = Schemas['HealthResponse']
export type HistoryRange = Schemas['HistoryRange']
export type HistoryResponse = Schemas['HistoryResponse']
export type HistoryBucket = Schemas['BucketResponse']
export type Hypothesis = NonNullable<Schemas['Hypothesis']>
export type HypothesisScore = Schemas['HypothesisScoreResponse']
export type IncidentDetail = Schemas['IncidentDetailResponse']
export type IncidentSummary = Schemas['IncidentSummaryResponse']
export type LiveEvent = Schemas['LiveEvent']
export type LiveResponse = Schemas['LiveResponse']
export type LiveState = Schemas['LiveState']
export type RangeResponse = Schemas['RangeResponse']
export type RoleSeries = Schemas['RoleSeriesResponse']
export type SensorResponse = Schemas['SensorResponse']
export type SensorRole = Schemas['SensorRole']
export type SessionResponse = Schemas['SessionResponse']
export type TelemetryResponse = Schemas['TelemetryResponse']
export type StorageState = Schemas['StorageState']
