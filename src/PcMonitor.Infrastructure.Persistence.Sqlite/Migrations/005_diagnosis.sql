-- One diagnosis per incident, replaced when the rules (engine version) change.

CREATE TABLE diagnosis (
    incident_id      INTEGER PRIMARY KEY REFERENCES incident (id),
    engine_version   INTEGER NOT NULL,
    diagnosed_at_ms  INTEGER NOT NULL,
    top_hypothesis   TEXT,
    confidence       TEXT    NOT NULL,
    ranking_json     TEXT    NOT NULL,
    findings_json    TEXT    NOT NULL,
    limitations_json TEXT    NOT NULL
);

-- The sleep state was stored as 0/1; recover the real power state (e.g. 4 = hibernate/Fast Startup) from the event data.
UPDATE incident
SET sleep_in_progress = CAST(json_extract(data_json, '$.SleepInProgress') AS INTEGER)
WHERE json_extract(data_json, '$.SleepInProgress') IS NOT NULL;
