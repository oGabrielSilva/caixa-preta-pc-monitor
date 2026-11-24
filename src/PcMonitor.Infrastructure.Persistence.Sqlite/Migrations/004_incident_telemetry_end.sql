-- Retention protects the frames just before the end of each incident's telemetry, across every segment of the
-- session that went down (a gap can split those last minutes). Existing incidents are backfilled.

ALTER TABLE incident ADD COLUMN telemetry_end_ms INTEGER;

UPDATE incident
SET telemetry_end_ms = (SELECT MAX(g.last_frame_ms) FROM segment g WHERE g.session_id = incident.session_id)
WHERE session_id IS NOT NULL;

CREATE INDEX ix_incident_session ON incident (session_id);
