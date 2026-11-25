-- Hourly summaries, kept forever. Minute aggregates are deleted after the retention period once their hour is
-- summarised here; the summary is per sensor, independent of the segments it was recorded in.
CREATE TABLE aggregate_1h (
    sensor_id INTEGER NOT NULL REFERENCES sensor (id),
    hour_ms   INTEGER NOT NULL,
    min       REAL    NOT NULL,
    avg       REAL    NOT NULL,
    max       REAL    NOT NULL,
    PRIMARY KEY (sensor_id, hour_ms)
) WITHOUT ROWID;
