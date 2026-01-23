-- Restore Script: Daten aus Backup-Tabellen in neue Schema-Struktur wiederherstellen
-- Dieser Script sollte NACH dem Anwenden der EF Core Migrations ausgeführt werden

BEGIN;

DO $$
BEGIN
    -- Prüfen, ob Backup-Tabellen existieren
    IF EXISTS (SELECT FROM information_schema.tables
               WHERE table_schema = 'public'
               AND table_name = '_backup_companies') THEN

        RAISE NOTICE 'Backup-Tabellen gefunden - starte Restore...';

        -- 1. Companies wiederherstellen (host-Schema)
        INSERT INTO host."Companies" (
            "Id", "OwnerFullName", "BusinessName", "Address", "PostalCode", "City",
            "Country", "TaxNumber", "VatId", "IsKleinunternehmer", "BankName",
            "BankAccount", "Bic", "AccountHolder", "Email", "Phone", "Website",
            "DefaultPaymentTermDays", "InvoiceNumberPrefix", "FooterText",
            "LogoData", "LogoContentType", "EndpointId", "EndpointSchemeId",
            "SmtpHost", "SmtpPort", "SmtpUseSsl", "SmtpUsername", "SmtpPassword",
            "EmailSenderEmail", "EmailSenderName", "EmailSignature",
            "EmailLayout", "EmailPrimaryColor", "EmailAccentColor",
            "EmailGreeting", "EmailClosing",
            "PdfLayout", "PdfPrimaryColor", "PdfAccentColor",
            "PdfHeaderText", "PdfFooterText", "PdfPaymentNotice",
            "CreatedAt", "UpdatedAt"
        )
        SELECT
            "Id", "OwnerFullName", "BusinessName", "Address", "PostalCode", "City",
            "Country", "TaxNumber", "VatId", "IsKleinunternehmer", "BankName",
            "BankAccount", "Bic", "AccountHolder", "Email", "Phone", "Website",
            "DefaultPaymentTermDays", "InvoiceNumberPrefix", "FooterText",
            "LogoData", "LogoContentType", "EndpointId", "EndpointSchemeId",
            "SmtpHost", "SmtpPort", "SmtpUseSsl", "SmtpUsername", "SmtpPassword",
            "EmailSenderEmail", "EmailSenderName", "EmailSignature",
            COALESCE("EmailLayout", 1), -- Default: Klar
            COALESCE("EmailPrimaryColor", '#0F2A3D'),
            COALESCE("EmailAccentColor", '#4A90E2'),
            "EmailGreeting", "EmailClosing",
            COALESCE("PdfLayout", 1), -- Default: Klar
            COALESCE("PdfPrimaryColor", '#1f3a5f'),
            COALESCE("PdfAccentColor", '#4A90E2'),
            "PdfHeaderText", "PdfFooterText", "PdfPaymentNotice",
            "CreatedAt", "UpdatedAt"
        FROM public._backup_companies;

        RAISE NOTICE 'Companies wiederhergestellt.';

        -- 2. Customers wiederherstellen (host-Schema)
        INSERT INTO host."Customers" (
            "Id", "CustomerNumber", "Name", "Address", "PostalCode", "City",
            "Country", "Email", "Phone", "Notes", "CreatedAt", "UpdatedAt"
        )
        SELECT
            "Id", "CustomerNumber", "Name", "Address", "PostalCode", "City",
            "Country", "Email", "Phone", "Notes", "CreatedAt", "UpdatedAt"
        FROM public._backup_customers;

        RAISE NOTICE 'Customers wiederhergestellt.';

        -- 3. Invoices wiederherstellen (faktura-Schema)
        INSERT INTO faktura."Invoices" (
            "Id", "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd",
            "DueDate", "CustomerId", "Notes", "Status", "PaidDate",
            "CreatedAt", "UpdatedAt", "EmailSentAt", "EmailSentTo", "EmailSendCount",
            "EmailCcRecipients", "EmailBccRecipients", "PrintedAt", "PrintCount",
            "DiscountType", "DiscountValue"
        )
        SELECT
            "Id", "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd",
            "DueDate", "CustomerId", "Notes", "Status", "PaidDate",
            "CreatedAt", "UpdatedAt", "EmailSentAt", "EmailSentTo", "EmailSendCount",
            "EmailCcRecipients", "EmailBccRecipients", "PrintedAt", "PrintCount",
            "DiscountType", "DiscountValue"
        FROM public._backup_invoices;

        RAISE NOTICE 'Invoices wiederhergestellt.';

        -- 4. InvoiceItems wiederherstellen (faktura-Schema)
        INSERT INTO faktura."InvoiceItems" (
            "Id", "InvoiceId", "Position", "Description", "Quantity",
            "UnitPrice", "VatRate"
        )
        SELECT
            "Id", "InvoiceId", "Position", "Description", "Quantity",
            "UnitPrice", "VatRate"
        FROM public._backup_invoiceitems;

        RAISE NOTICE 'InvoiceItems wiederhergestellt.';

        -- 5. DownPayments wiederherstellen (faktura-Schema)
        INSERT INTO faktura."DownPayments" (
            "Id", "InvoiceId", "Description", "Amount", "PaymentDate", "CreatedAt"
        )
        SELECT
            "Id", "InvoiceId", "Description", "Amount", "PaymentDate", "CreatedAt"
        FROM public._backup_downpayments;

        RAISE NOTICE 'DownPayments wiederhergestellt.';

        -- Sequenzen aktualisieren
        SELECT setval('host."Companies_Id_seq"', (SELECT MAX("Id") FROM host."Companies"));
        SELECT setval('host."Customers_Id_seq"', (SELECT MAX("Id") FROM host."Customers"));
        SELECT setval('faktura."Invoices_Id_seq"', (SELECT MAX("Id") FROM faktura."Invoices"));
        SELECT setval('faktura."InvoiceItems_Id_seq"', (SELECT MAX("Id") FROM faktura."InvoiceItems"));
        SELECT setval('faktura."DownPayments_Id_seq"', (SELECT MAX("Id") FROM faktura."DownPayments"));

        RAISE NOTICE 'Sequenzen aktualisiert.';

        -- Backup-Tabellen entfernen (optional - für Sicherheit erstmal behalten)
        -- DROP TABLE IF EXISTS public._backup_companies;
        -- DROP TABLE IF EXISTS public._backup_customers;
        -- DROP TABLE IF EXISTS public._backup_invoices;
        -- DROP TABLE IF EXISTS public._backup_invoiceitems;
        -- DROP TABLE IF EXISTS public._backup_downpayments;

        RAISE NOTICE 'Migration abgeschlossen! Backup-Tabellen bleiben zur Sicherheit erhalten.';
        RAISE NOTICE 'Sie können sie manuell löschen, wenn alles funktioniert.';

    ELSE
        RAISE NOTICE 'Keine Backup-Tabellen gefunden - nichts zu restoren.';
    END IF;
END $$;

COMMIT;
