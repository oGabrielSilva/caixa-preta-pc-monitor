-- The history view reads one sensor over a period; aggregates are kept forever, so that read needs an index
-- (the primary key starts with the segment).
CREATE INDEX ix_aggregate_sensor_minute ON aggregate_1m (sensor_id, minute_ms);
