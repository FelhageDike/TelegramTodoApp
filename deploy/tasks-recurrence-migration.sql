-- Run once on existing tasks_db if the database was created before recurrence columns:
-- docker exec -i deploy-postgres-1 psql -U tgtodo -d tasks_db < deploy/tasks-recurrence-migration.sql

ALTER TABLE tasks ADD COLUMN IF NOT EXISTS "DayOfMonth" integer;
ALTER TABLE tasks ADD COLUMN IF NOT EXISTS "IntervalDays" integer;
ALTER TABLE tasks ADD COLUMN IF NOT EXISTS "RecurrenceStartDate" date;
ALTER TABLE tasks ADD COLUMN IF NOT EXISTS "Weekday" integer;

UPDATE tasks SET "RecurrenceStartDate" = CURRENT_DATE WHERE "RecurrenceStartDate" IS NULL;
