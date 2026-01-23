-- Migration Script: Bestehende Daten von public-Schema in host/faktura-Schemas migrieren
-- Dieser Script sollte VOR dem Anwenden der EF Core Migrations ausgeführt werden,
-- wenn bereits eine Datenbank mit Daten im public-Schema existiert.

-- WICHTIG: Dieser Script ist OPTIONAL und nur notwendig, wenn bereits Daten vorhanden sind!

BEGIN;

-- 1. Prüfen, ob die alten Tabellen existieren
DO $$
BEGIN
    -- Wenn Companies im public-Schema existiert, migriere die Daten
    IF EXISTS (SELECT FROM information_schema.tables
               WHERE table_schema = 'public'
               AND table_name = 'Companies') THEN

        RAISE NOTICE 'Alte Tabellen gefunden - starte Migration...';

        -- Erstelle Schemas falls noch nicht vorhanden
        CREATE SCHEMA IF NOT EXISTS host;
        CREATE SCHEMA IF NOT EXISTS faktura;

        -- Erstelle temporäre Backup-Tabellen im public-Schema
        CREATE TABLE IF NOT EXISTS public._backup_companies AS SELECT * FROM public."Companies";
        CREATE TABLE IF NOT EXISTS public._backup_customers AS SELECT * FROM public."Customers";
        CREATE TABLE IF NOT EXISTS public._backup_invoices AS SELECT * FROM public."Invoices";
        CREATE TABLE IF NOT EXISTS public._backup_invoiceitems AS SELECT * FROM public."InvoiceItems";
        CREATE TABLE IF NOT EXISTS public._backup_downpayments AS SELECT * FROM public."DownPayments";

        RAISE NOTICE 'Backup erstellt.';

        -- Lösche die alten Tabellen
        DROP TABLE IF EXISTS public."DownPayments" CASCADE;
        DROP TABLE IF EXISTS public."InvoiceItems" CASCADE;
        DROP TABLE IF EXISTS public."Invoices" CASCADE;
        DROP TABLE IF EXISTS public."Customers" CASCADE;
        DROP TABLE IF EXISTS public."Companies" CASCADE;
        DROP TABLE IF EXISTS public."__EFMigrationsHistory" CASCADE;

        RAISE NOTICE 'Alte Tabellen gelöscht. Jetzt EF Core Migrations anwenden!';
        RAISE NOTICE 'Nach den Migrations führe den Restore-Script aus: restore-from-backup.sql';

    ELSE
        RAISE NOTICE 'Keine alten Tabellen gefunden - vermutlich eine Neuinstallation.';
    END IF;
END $$;

COMMIT;
