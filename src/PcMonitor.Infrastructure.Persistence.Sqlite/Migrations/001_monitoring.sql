-- Monitoring sessions, segments, the sensor catalog, raw frames and 1-minute aggregates (ADR 0004).

CREATE TABLE session (
    id            INTEGER PRIMARY KEY,
    started_at_ms INTEGER NOT NULL,
    booted_at_ms  INTEGER NOT NULL,
    source_status TEXT    NOT NULL,
    board_product TEXT,
    ended_at_ms   INTEGER,
    end_reason    TEXT
);

CREATE TABLE segment (
    id            INTEGER PRIMARY KEY,
    session_id    INTEGER NOT NULL REFERENCES session (id),
    started_at_ms INTEGER NOT NULL,
    last_frame_ms INTEGER,
    ended_at_ms   INTEGER,
    end_reason    TEXT
);

CREATE TABLE sensor (
    id         INTEGER PRIMARY KEY,
    stable_key TEXT NOT NULL UNIQUE,
    name       TEXT NOT NULL,
    kind       TEXT NOT NULL,
    unit       TEXT NOT NULL,
    role       TEXT NOT NULL
);

CREATE TABLE segment_sensor (
    segment_id INTEGER NOT NULL REFERENCES segment (id),
    ordinal    INTEGER NOT NULL,
    sensor_id  INTEGER NOT NULL REFERENCES sensor (id),
    PRIMARY KEY (segment_id, ordinal)
) WITHOUT ROWID;

-- rowid primary key and a non-unique (segment_id, ts_ms) index: frames with equal or
-- out-of-order timestamps (clock adjustments) are all kept.
CREATE TABLE frame (
    id         INTEGER PRIMARY KEY,
    segment_id INTEGER NOT NULL REFERENCES segment (id),
    ts_ms      INTEGER NOT NULL,
    "values"   BLOB    NOT NULL
);
CREATE INDEX ix_frame_segment_ts ON frame (segment_id, ts_ms);
CREATE INDEX ix_frame_ts ON frame (ts_ms);

CREATE TABLE aggregate_1m (
    segment_id INTEGER NOT NULL REFERENCES segment (id),
    minute_ms  INTEGER NOT NULL,
    sensor_id  INTEGER NOT NULL REFERENCES sensor (id),
    min        REAL,
    avg        REAL,
    max        REAL,
    PRIMARY KEY (segment_id, minute_ms, sensor_id)
) WITHOUT ROWID;
