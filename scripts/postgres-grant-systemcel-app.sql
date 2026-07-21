-- Run as doadmin on the systemcel database.
-- Grants the runtime application user the minimum schema-level ability needed
-- to let EF Core migrations create and update Systemcel tables.

GRANT USAGE, CREATE ON SCHEMA public TO systemcel_app;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO systemcel_app;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO systemcel_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON TABLES TO systemcel_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON SEQUENCES TO systemcel_app;
