-- Incidents: one per Kernel-Power 41 event (ADR 0005), with the telemetry segment when the collector was running,
-- and the event log entries around the failure.

CREATE TABLE incident (
    id                   INTEGER PRIMARY KEY,
    event_record_id      INTEGER NOT NULL,
    logged_at_ms         INTEGER NOT NULL,
    estimated_failure_ms INTEGER,
    failure_time_source  TEXT    NOT NULL,
    session_id           INTEGER REFERENCES session (id),
    segment_id           INTEGER REFERENCES segment (id),
    bugcheck_code        INTEGER NOT NULL,
    sleep_in_progress    INTEGER NOT NULL,
    power_button_ms      INTEGER,
    long_power_button    INTEGER NOT NULL,
    last_alive_ms        INTEGER,
    data_json            TEXT    NOT NULL,
    UNIQUE (event_record_id, logged_at_ms)
);
CREATE INDEX ix_incident_segment ON incident (segment_id);

CREATE TABLE incident_event (
    incident_id  INTEGER NOT NULL REFERENCES incident (id),
    provider     TEXT    NOT NULL,
    event_id     INTEGER NOT NULL,
    record_id    INTEGER NOT NULL,
    logged_at_ms INTEGER NOT NULL,
    level        INTEGER NOT NULL,
    data_json    TEXT    NOT NULL,
    PRIMARY KEY (incident_id, provider, record_id)
) WITHOUT ROWID;
