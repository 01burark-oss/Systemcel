-- DEVELOPMENT ONLY.
-- Run as doadmin on the systemcel database if the schema was previously
-- created with EnsureCreated and has no __EFMigrationsHistory table.
-- This deletes all tables and data in the public schema.

DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

GRANT ALL ON SCHEMA public TO doadmin;
GRANT USAGE, CREATE ON SCHEMA public TO systemcel_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON TABLES TO systemcel_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON SEQUENCES TO systemcel_app;
