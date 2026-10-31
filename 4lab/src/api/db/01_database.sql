-- =====================================================================
-- «Муркет» — 01: роль и база данных (запускать один раз под суперпользователем)
--
--   psql -h 127.0.0.1 -p 5432 -U postgres -d postgres -f 01_database.sql
--
-- Роль murket: владелец БД приложения; CREATEDB нужен интеграционным тестам,
-- которые создают изолированные тестовые базы (см. tests/integration).
-- =====================================================================

SELECT 'CREATE ROLE murket LOGIN CREATEDB PASSWORD ''murket'''
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'murket')\gexec

SELECT 'CREATE DATABASE murket OWNER murket'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'murket')\gexec

ALTER DATABASE murket OWNER TO murket;
