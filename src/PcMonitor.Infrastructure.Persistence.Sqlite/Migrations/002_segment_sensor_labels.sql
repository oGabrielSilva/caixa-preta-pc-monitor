-- Each segment keeps the name, kind, unit and role its sensors had when it was recorded, so a later profile
-- change can never relabel history. The sensor table becomes identity only (id, stable_key).

ALTER TABLE segment_sensor ADD COLUMN name TEXT NOT NULL DEFAULT '';
ALTER TABLE segment_sensor ADD COLUMN kind TEXT NOT NULL DEFAULT '';
ALTER TABLE segment_sensor ADD COLUMN unit TEXT NOT NULL DEFAULT '';
ALTER TABLE segment_sensor ADD COLUMN role TEXT NOT NULL DEFAULT 'None';

UPDATE segment_sensor
SET name = s.name, kind = s.kind, unit = s.unit, role = s.role
FROM sensor s
WHERE s.id = segment_sensor.sensor_id;

ALTER TABLE sensor DROP COLUMN name;
ALTER TABLE sensor DROP COLUMN kind;
ALTER TABLE sensor DROP COLUMN unit;
ALTER TABLE sensor DROP COLUMN role;
