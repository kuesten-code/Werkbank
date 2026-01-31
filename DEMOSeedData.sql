-- Seed Data für InvoiceApp Screenshots
-- Erstellt realistische deutsche Musterdaten

-- Company (Eigene Firmendaten)
INSERT INTO "Companies" (
    "OwnerFullName", "BusinessName", "Address", "PostalCode", "City", "Country",
    "TaxNumber", "VatId", "IsKleinunternehmer", "BankName", "BankAccount", "Bic", "AccountHolder",
    "Email", "Phone", "Website", "DefaultPaymentTermDays", "InvoiceNumberPrefix", "FooterText",
    "CreatedAt", "UpdatedAt",
    "SmtpUseSsl",
    "PdfLayout", "PdfPrimaryColor", "PdfAccentColor",
    "EmailLayout", "EmailPrimaryColor", "EmailAccentColor"
) VALUES (
    'Maximilian Weber', 'Weber IT-Consulting', 'Hauptstraße 42', '80331', 'München', 'Deutschland',
    '143/123/45678', 'DE123456789', false, 'Sparkasse München', 'DE89370400440532013000', 'COBADEFFXXX', 'Maximilian Weber',
    'kontakt@weber-it.de', '+49 89 123456', 'https://www.weber-it.de', 14, 'WIT-',
    'Vielen Dank für Ihr Vertrauen! Bei Fragen stehen wir Ihnen gerne zur Verfügung.',
    NOW(), NOW(),
    true,
    1, '#1f3a5f', '#3FA796',
    1, '#0F2A3D', '#3FA796'
);

-- Kunden
INSERT INTO "Customers" ("CustomerNumber", "Name", "Address", "PostalCode", "City", "Country", "Email", "Phone", "Notes", "CreatedAt", "UpdatedAt") VALUES
('K-001', 'Müller & Söhne GmbH', 'Industriestraße 15', '70173', 'Stuttgart', 'Deutschland', 'info@mueller-soehne.de', '+49 711 987654', 'Großkunde seit 2020, bevorzugt Zahlungsziel 30 Tage', NOW(), NOW()),
('K-002', 'Schmidt Maschinenbau AG', 'Werksweg 8', '90402', 'Nürnberg', 'Deutschland', 'einkauf@schmidt-mb.de', '+49 911 456789', 'Regelmäßige IT-Wartungsverträge', NOW(), NOW()),
('K-003', 'Dr. Anna Becker', 'Praxisstraße 23', '80333', 'München', 'Deutschland', 'praxis@dr-becker.de', '+49 89 234567', 'Arztpraxis, monatliche IT-Betreuung', NOW(), NOW()),
('K-004', 'Hofmann Elektrotechnik', 'Gewerbepark 5', '85748', 'Garching', 'Deutschland', 'kontakt@hofmann-elektro.de', '+49 89 345678', NULL, NOW(), NOW()),
('K-005', 'Gasthaus Zum Goldenen Hirsch', 'Marktplatz 1', '82467', 'Garmisch-Partenkirchen', 'Deutschland', 'info@goldener-hirsch.de', '+49 8821 12345', 'Kassensystem und Website-Betreuung', NOW(), NOW()),
('K-006', 'Fischer Steuerberatung', 'Kanzleiweg 12', '81675', 'München', 'Deutschland', 'kanzlei@fischer-stb.de', '+49 89 567890', 'Datev-Integration erforderlich', NOW(), NOW()),
('K-007', 'Autohaus Brenner', 'Autobahnstraße 100', '85221', 'Dachau', 'Deutschland', 'service@brenner-auto.de', '+49 8131 98765', 'Werkstattsoftware-Support', NOW(), NOW()),
('K-008', 'Bäckerei Tradition Huber', 'Bahnhofstraße 7', '82515', 'Wolfratshausen', 'Deutschland', 'info@baeckerei-huber.de', '+49 8171 54321', 'Filialsystem mit 3 Standorten', NOW(), NOW());

-- Rechnungen mit verschiedenen Status
-- Rechnung 1: Bezahlt (vor 2 Monaten)
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-001', '2024-10-15', '2024-10-01', '2024-10-31', '2024-10-29',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'),
    'Monatliche IT-Wartung Oktober 2024', 2, '2024-10-25', NOW(), NOW(),
    '2024-10-15 10:30:00', 'info@mueller-soehne.de', 1, 1, 0, NULL
);

-- Rechnung 2: Bezahlt (letzten Monat)
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-002', '2024-11-01', '2024-11-01', '2024-11-30', '2024-11-15',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-003'),
    'IT-Betreuung Arztpraxis November', 2, '2024-11-12', NOW(), NOW(),
    '2024-11-01 09:15:00', 'praxis@dr-becker.de', 1, 0, 0, NULL
);

-- Rechnung 3: Bezahlt mit Rabatt
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-003', '2024-11-10', NULL, NULL, '2024-11-24',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'),
    'Server-Migration und Netzwerkoptimierung', 2, '2024-11-20', NOW(), NOW(),
    '2024-11-10 14:00:00', 'einkauf@schmidt-mb.de', 1, 2, 1, 5.00
);

-- Rechnung 4: Versendet (offen)
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-004', '2024-12-01', '2024-12-01', '2024-12-31', '2024-12-15',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'),
    'Monatliche IT-Wartung Dezember 2024', 1, NULL, NOW(), NOW(),
    '2024-12-01 08:45:00', 'info@mueller-soehne.de', 1, 0, 0, NULL
);

-- Rechnung 5: Überfällig
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-005', '2024-11-15', NULL, NULL, '2024-11-29',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-007'),
    'Werkstattsoftware-Update und Schulung', 3, NULL, NOW(), NOW(),
    '2024-11-15 11:20:00', 'service@brenner-auto.de', 2, 1, 0, NULL
);

-- Rechnung 6: Entwurf
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-006', '2024-12-10', NULL, NULL, '2024-12-24',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-005'),
    'Website-Relaunch und Kassensystem-Integration', 0, NULL, NOW(), NOW(),
    NULL, NULL, 0, 0, 1, 10.00
);

-- Rechnung 7: Versendet (offen)
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-007', '2024-12-05', '2024-12-01', '2024-12-31', '2024-12-19',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-006'),
    'Datev-Schnittstelle Einrichtung', 1, NULL, NOW(), NOW(),
    '2024-12-05 16:30:00', 'kanzlei@fischer-stb.de', 1, 0, 0, NULL
);

-- Rechnung 8: Storniert
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-008', '2024-11-20', NULL, NULL, '2024-12-04',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-004'),
    'Storniert - Projekt verschoben auf Q1 2025', 4, NULL, NOW(), NOW(),
    '2024-11-20 13:00:00', 'kontakt@hofmann-elektro.de', 1, 0, 0, NULL
);

-- Rechnung 9: Bezahlt mit Anzahlung
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-009', '2024-11-25', NULL, NULL, '2024-12-09',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-008'),
    'Filialsystem-Erweiterung auf 3. Standort', 2, '2024-12-08', NOW(), NOW(),
    '2024-11-25 10:00:00', 'info@baeckerei-huber.de', 1, 1, 0, NULL
);

-- Rechnung 10: Entwurf (aktuell in Bearbeitung)
INSERT INTO "Invoices" (
    "InvoiceNumber", "InvoiceDate", "ServicePeriodStart", "ServicePeriodEnd", "DueDate",
    "CustomerId", "Notes", "Status", "PaidDate", "CreatedAt", "UpdatedAt",
    "EmailSentAt", "EmailSentTo", "EmailSendCount", "PrintCount", "DiscountType", "DiscountValue"
) VALUES (
    'WIT-2024-010', '2024-12-12', '2024-12-01', '2024-12-31', '2024-12-26',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-003'),
    'IT-Betreuung Arztpraxis Dezember', 0, NULL, NOW(), NOW(),
    NULL, NULL, 0, 0, 0, NULL
);

-- Rechnungspositionen
-- Rechnung 1: IT-Wartung
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'IT-Wartung Pauschal (monatlich)', 1, 850.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-001';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Remote-Support (Stunden)', 3.5, 95.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-001';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Hardware: USB-C Docking Station', 2, 189.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-001';

-- Rechnung 2: Arztpraxis
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'IT-Betreuung Arztpraxis (monatlich)', 1, 450.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-002';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Backup-Service Cloud (monatlich)', 1, 79.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-002';

-- Rechnung 3: Server-Migration (mit Rabatt)
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Server-Migration (Planung & Durchführung)', 1, 2400.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-003';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Netzwerk-Optimierung', 8, 95.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-003';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Dokumentation & Schulung', 4, 85.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-003';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 4, 'Hardware: Server Dell PowerEdge T150', 1, 1850.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-003';

-- Rechnung 4: Dezember Wartung
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'IT-Wartung Pauschal (monatlich)', 1, 850.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-004';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Windows 11 Upgrade (5 Arbeitsplätze)', 5, 45.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-004';

-- Rechnung 5: Überfällig - Autohaus
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Werkstattsoftware-Update auf Version 5.0', 1, 590.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-005';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Mitarbeiterschulung (vor Ort)', 4, 120.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-005';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Anfahrtspauschale', 1, 45.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-005';

-- Rechnung 6: Entwurf - Gasthaus (mit 10% Rabatt)
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Website-Relaunch (Design & Entwicklung)', 1, 2800.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-006';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Kassensystem-Integration', 1, 450.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-006';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Online-Reservierungssystem', 1, 380.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-006';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 4, 'SEO-Grundoptimierung', 1, 290.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-006';

-- Rechnung 7: Steuerberater Datev
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Datev-Schnittstelle Einrichtung', 1, 680.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-007';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Konfiguration & Test', 3, 95.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-007';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Dokumentation', 1, 150.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-007';

-- Rechnung 8: Storniert
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Elektroinstallation IT-Verkabelung', 1, 1200.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-008';

-- Rechnung 9: Bäckerei mit Anzahlung
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'Filialsystem-Erweiterung (3. Standort)', 1, 1800.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-009';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Hardware: Kassen-Terminal', 1, 890.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-009';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Installation & Einrichtung vor Ort', 6, 95.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-009';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 4, 'Mitarbeiterschulung', 2, 120.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-009';

-- Rechnung 10: Entwurf Arztpraxis Dezember
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 1, 'IT-Betreuung Arztpraxis (monatlich)', 1, 450.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-010';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 2, 'Backup-Service Cloud (monatlich)', 1, 79.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-010';
INSERT INTO "InvoiceItems" ("InvoiceId", "Position", "Description", "Quantity", "UnitPrice", "VatRate")
SELECT "Id", 3, 'Praxissoftware-Update', 1, 180.00, 19.00 FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-010';

-- Anzahlungen für Rechnung 9
INSERT INTO "DownPayments" ("InvoiceId", "Description", "Amount", "PaymentDate", "CreatedAt")
SELECT "Id", 'Anzahlung bei Auftragserteilung', 1000.00, '2024-11-10', NOW() FROM "Invoices" WHERE "InvoiceNumber" = 'WIT-2024-009';

-- =====================================================
-- RAPPORT MODULE SEED DATA
-- =====================================================

-- Rapport Settings
INSERT INTO rapport."Settings" (
    "Id", "DefaultHourlyRate", "ShowHourlyRateInPdf", "CalculateTotalAmount",
    "RoundingMinutes", "StartOfWeek", "DefaultProjectId", "AutoStopTimerAfterHours",
    "EnableSounds", "PdfLayout", "PdfPrimaryColor", "PdfAccentColor", "PdfHeaderText", "PdfFooterText"
) VALUES (
    1, 95.00, true, true,
    15, 1, NULL, 10,
    false, 1, '#1f3a5f', '#3FA796', 'Tätigkeitsnachweis', 'Vielen Dank für die gute Zusammenarbeit!'
);

-- Zeiterfassungen für verschiedene Kunden
-- Letzten Monat: Müller & Söhne GmbH - IT-Wartung
INSERT INTO rapport."TimeEntries" (
    "Id", "StartTime", "EndTime", "Description", "IsManual", "CustomerId", "CustomerName",
    "ProjectId", "ProjectName", "Status", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt"
) VALUES
-- Kalenderwoche 48 (Ende November)
(gen_random_uuid(), '2024-11-25 08:30:00', '2024-11-25 10:45:00', 'Server-Monitoring und Backup-Prüfung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'), 'Müller & Söhne GmbH', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-11-25 11:00:00', '2024-11-25 12:30:00', 'Windows Updates auf 5 Arbeitsplätzen', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'), 'Müller & Söhne GmbH', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-11-26 09:00:00', '2024-11-26 11:30:00', 'Netzwerk-Troubleshooting', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'), 'Müller & Söhne GmbH', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Kalenderwoche 48: Schmidt Maschinenbau AG - Server-Migration
(gen_random_uuid(), '2024-11-26 13:00:00', '2024-11-26 17:30:00', 'Server-Migration Phase 1: Datensicherung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'), 'Schmidt Maschinenbau AG', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-11-27 08:00:00', '2024-11-27 16:00:00', 'Server-Migration Phase 2: Installation neuer Server', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'), 'Schmidt Maschinenbau AG', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-11-28 08:00:00', '2024-11-28 12:00:00', 'Server-Migration Phase 3: Datenmigration', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'), 'Schmidt Maschinenbau AG', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-11-28 13:00:00', '2024-11-28 17:00:00', 'Server-Migration Phase 4: Tests und Feinabstimmung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'), 'Schmidt Maschinenbau AG', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Kalenderwoche 49 (Anfang Dezember)
-- Dr. Anna Becker - Arztpraxis IT-Betreuung
(gen_random_uuid(), '2024-12-02 08:00:00', '2024-12-02 09:30:00', 'Praxissoftware-Update und Test', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-003'), 'Dr. Anna Becker', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-02 10:00:00', '2024-12-02 11:00:00', 'Backup-Prüfung und Datensicherung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-003'), 'Dr. Anna Becker', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Fischer Steuerberatung - Datev-Integration
(gen_random_uuid(), '2024-12-03 09:00:00', '2024-12-03 12:30:00', 'Datev-Schnittstelle Einrichtung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-006'), 'Fischer Steuerberatung', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-03 13:30:00', '2024-12-03 16:00:00', 'Datev-Import Tests und Konfiguration', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-006'), 'Fischer Steuerberatung', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-04 10:00:00', '2024-12-04 12:00:00', 'Mitarbeiterschulung Datev-Export', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-006'), 'Fischer Steuerberatung', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Bäckerei Tradition Huber - Filialsystem
(gen_random_uuid(), '2024-12-05 07:00:00', '2024-12-05 12:00:00', 'Kassensystem-Installation Filiale 3', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-008'), 'Bäckerei Tradition Huber', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-05 13:00:00', '2024-12-05 16:30:00', 'Netzwerkanbindung und Synchronisation', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-008'), 'Bäckerei Tradition Huber', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-06 08:00:00', '2024-12-06 10:00:00', 'Mitarbeiterschulung Kassensystem', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-008'), 'Bäckerei Tradition Huber', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Kalenderwoche 50
-- Autohaus Brenner - Werkstattsoftware
(gen_random_uuid(), '2024-12-09 09:00:00', '2024-12-09 12:00:00', 'Werkstattsoftware-Update auf v5.0', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-007'), 'Autohaus Brenner', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-09 13:00:00', '2024-12-09 17:00:00', 'Schulung Werkstattmitarbeiter', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-007'), 'Autohaus Brenner', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Müller & Söhne - Dezember Wartung
(gen_random_uuid(), '2024-12-10 08:30:00', '2024-12-10 11:00:00', 'Monatliche IT-Wartung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'), 'Müller & Söhne GmbH', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-10 11:30:00', '2024-12-10 13:00:00', 'Virenscan und Sicherheitscheck', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'), 'Müller & Söhne GmbH', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Gasthaus Goldener Hirsch - Website
(gen_random_uuid(), '2024-12-11 09:00:00', '2024-12-11 12:00:00', 'Website-Relaunch: Konzeptbesprechung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-005'), 'Gasthaus Zum Goldenen Hirsch', NULL, NULL, 1, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-12 09:00:00', '2024-12-12 17:00:00', 'Website-Relaunch: Design-Umsetzung', false,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-005'), 'Gasthaus Zum Goldenen Hirsch', NULL, NULL, 1, false, NULL, NOW(), NOW()),

-- Manuell erfasste Einträge (retrospektiv)
(gen_random_uuid(), '2024-12-06 14:00:00', '2024-12-06 15:30:00', 'Telefonische Beratung und Remote-Support', true,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-003'), 'Dr. Anna Becker', NULL, NULL, 2, false, NULL, NOW(), NOW()),
(gen_random_uuid(), '2024-12-11 14:00:00', '2024-12-11 15:00:00', 'Telefonische Supportanfrage', true,
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'), 'Schmidt Maschinenbau AG', NULL, NULL, 2, false, NULL, NOW(), NOW());

-- =====================================================
-- OFFERTE MODULE SEED DATA
-- =====================================================

-- Offerte Settings
INSERT INTO offerte."Settings" (
    "Id", "EmailLayout", "EmailPrimaryColor", "EmailAccentColor", "EmailGreeting", "EmailClosing",
    "PdfLayout", "PdfPrimaryColor", "PdfAccentColor", "PdfHeaderText", "PdfFooterText", "PdfValidityNotice"
) VALUES (
    1, 'Strukturiert', '#0F2A3D', '#3FA796',
    'Sehr geehrte Damen und Herren,

vielen Dank für Ihr Interesse an unseren Dienstleistungen. Anbei erhalten Sie unser Angebot.',
    'Mit freundlichen Grüßen

{{Firmenname}}',
    'Strukturiert', '#1f3a5f', '#3FA796',
    'Angebot',
    'Wir freuen uns auf eine erfolgreiche Zusammenarbeit!',
    'Dieses Angebot ist gültig bis zum {{Gueltigkeitsdatum}}. Bei Fragen stehen wir Ihnen gerne zur Verfügung.'
);

-- Angebot 1: Angenommen - Server-Aufrüstung (vor 2 Monaten)
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-001',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-002'),
    'Angenommen', '2024-10-15', '2024-11-15',
    'Projekt: Server-Modernisierung', 'Kunde hat Angebot am 25.10. angenommen',
    'Gerne unterbreiten wir Ihnen folgendes Angebot für die Server-Modernisierung in Ihrem Unternehmen.',
    'Wir freuen uns auf Ihren Auftrag und stehen für Rückfragen gerne zur Verfügung.',
    NOW(), NOW(), '2024-10-15 10:00:00', '2024-10-25 14:30:00', NULL, NULL,
    '2024-10-15 10:00:00', 'einkauf@schmidt-mb.de', 1, '2024-10-15 09:45:00', 1
);

-- Angebot 2: Abgelehnt - Website-Projekt
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-002',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-004'),
    'Abgelehnt', '2024-10-20', '2024-11-20',
    'Anfrage Website', 'Kunde hat sich für anderen Anbieter entschieden',
    'Vielen Dank für Ihre Anfrage. Gerne erstellen wir Ihnen ein individuelles Angebot.',
    NULL,
    NOW(), NOW(), '2024-10-20 14:00:00', NULL, '2024-11-05 09:00:00', NULL,
    '2024-10-20 14:00:00', 'kontakt@hofmann-elektro.de', 1, NULL, 0
);

-- Angebot 3: Versendet - IT-Wartungsvertrag (offen)
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-003',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-006'),
    'Versendet', '2024-12-01', '2024-12-31',
    'IT-Wartungsvertrag 2025', 'Nachfassen am 15.12. geplant',
    'Gerne bieten wir Ihnen einen umfassenden IT-Wartungsvertrag für das Jahr 2025 an.',
    'Bei Vertragsabschluss bis zum 20.12.2024 gewähren wir 5% Frühbucherrabatt.',
    NOW(), NOW(), '2024-12-01 11:00:00', NULL, NULL, NULL,
    '2024-12-01 11:00:00', 'kanzlei@fischer-stb.de', 1, '2024-12-01 10:30:00', 1
);

-- Angebot 4: Entwurf - Kassensystem-Erweiterung
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-004',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-005'),
    'Entwurf', '2024-12-10', '2025-01-10',
    'Anfrage Kassensystem', 'Noch Klärung der genauen Anforderungen nötig',
    'Basierend auf unserem Gespräch erstellen wir Ihnen gerne folgendes Angebot.',
    NULL,
    NOW(), NOW(), NULL, NULL, NULL, NULL,
    NULL, NULL, 0, NULL, 0
);

-- Angebot 5: Versendet - Netzwerk-Ausbau
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-005',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-001'),
    'Versendet', '2024-12-05', '2025-01-05',
    'Projekt: Netzwerk-Modernisierung', 'Entscheidung wird im Januar erwartet',
    'Wie besprochen unterbreiten wir Ihnen unser Angebot zur Modernisierung Ihrer Netzwerkinfrastruktur.',
    'Die Arbeiten können flexibel geplant werden, um den Betriebsablauf minimal zu beeinträchtigen.',
    NOW(), NOW(), '2024-12-05 09:00:00', NULL, NULL, NULL,
    '2024-12-05 09:00:00', 'info@mueller-soehne.de', 1, NULL, 0
);

-- Angebot 6: Abgelaufen - Schulungspaket
INSERT INTO offerte."Angebote" (
    "Id", "Angebotsnummer", "KundeId", "Status", "Erstelldatum", "GueltigBis",
    "Referenz", "Bemerkungen", "Einleitung", "Schlusstext",
    "CreatedAt", "UpdatedAt", "VersendetAm", "AngenommenAm", "AbgelehntAm", "AbgelaufenAm",
    "EmailGesendetAm", "EmailGesendetAn", "EmailAnzahl", "GedrucktAm", "DruckAnzahl"
) VALUES (
    gen_random_uuid(), 'AN-2024-006',
    (SELECT "Id" FROM "Customers" WHERE "CustomerNumber" = 'K-007'),
    'Abgelaufen', '2024-09-01', '2024-09-30',
    'IT-Schulungen Q4', 'Keine Rückmeldung erhalten',
    'Gerne bieten wir Ihnen ein maßgeschneidertes Schulungspaket für Ihre Mitarbeiter an.',
    NULL,
    NOW(), NOW(), '2024-09-01 10:00:00', NULL, NULL, '2024-10-01 00:00:00',
    '2024-09-01 10:00:00', 'service@brenner-auto.de', 2, NULL, 0
);

-- Angebotspositionen

-- AN-2024-001: Server-Aufrüstung (Angenommen)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'Dell PowerEdge T150 Server', 1, 1850.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-001';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'Windows Server 2022 Standard Lizenz', 1, 890.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-001';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Server-Migration und Einrichtung', 8, 95.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-001';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 4, 'Dokumentation und Schulung', 3, 85.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-001';

-- AN-2024-002: Website-Projekt (Abgelehnt)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'Website-Design (responsive)', 1, 1500.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-002';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'Entwicklung und Programmierung', 20, 85.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-002';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Content-Management-System Einrichtung', 1, 450.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-002';

-- AN-2024-003: IT-Wartungsvertrag 2025 (Versendet)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'IT-Wartungspauschale (monatlich)', 12, 350.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-003';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'Backup-Service Cloud (monatlich)', 12, 49.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-003';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Antivirus-Lizenz (5 Arbeitsplätze)', 1, 280.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-003';

-- AN-2024-004: Kassensystem-Erweiterung (Entwurf)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'Kassensystem-Integration', 1, 890.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-004';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'Online-Reservierungssystem', 1, 650.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-004';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Einrichtung und Schulung', 4, 95.00, 19.00, 10.00 FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-004';

-- AN-2024-005: Netzwerk-Ausbau (Versendet)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'Netzwerk-Switch 24-Port managed', 2, 420.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-005';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'WLAN Access Points', 3, 189.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-005';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Cat7 Verkabelung (Material)', 1, 650.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-005';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 4, 'Installation und Konfiguration', 12, 95.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-005';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 5, 'Netzwerk-Dokumentation', 1, 350.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-005';

-- AN-2024-006: Schulungspaket (Abgelaufen)
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 1, 'Microsoft 365 Grundlagen (Gruppenschulung)', 1, 890.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-006';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 2, 'IT-Sicherheit Awareness Training', 1, 650.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-006';
INSERT INTO offerte."Angebotspositionen" ("Id", "AngebotId", "Position", "Text", "Menge", "Einzelpreis", "Steuersatz", "Rabatt")
SELECT gen_random_uuid(), "Id", 3, 'Schulungsunterlagen (digital)', 10, 25.00, 19.00, NULL FROM offerte."Angebote" WHERE "Angebotsnummer" = 'AN-2024-006';

-- Fertig!
-- Zusammenfassung:
-- 1 Firma (Weber IT-Consulting)
-- 8 Kunden
-- 10 Rechnungen (verschiedene Status: 3 bezahlt, 2 offen, 1 überfällig, 2 Entwürfe, 1 storniert, 1 mit Anzahlung)
-- Diverse Rechnungspositionen mit realistischen IT-Dienstleistungen
--
-- RAPPORT MODULE:
-- 1 Settings-Eintrag (mit Stundensatz 95€)
-- 23 Zeiterfassungen für verschiedene Kunden über mehrere Wochen
-- - Müller & Söhne: IT-Wartung
-- - Schmidt Maschinenbau: Server-Migration
-- - Dr. Anna Becker: Praxis-IT
-- - Fischer Steuerberatung: Datev-Integration
-- - Bäckerei Huber: Filialsystem
-- - Autohaus Brenner: Werkstattsoftware
-- - Gasthaus Goldener Hirsch: Website
--
-- OFFERTE MODULE:
-- 1 Settings-Eintrag (E-Mail und PDF Konfiguration)
-- 6 Angebote (verschiedene Status: 1 angenommen, 1 abgelehnt, 2 versendet, 1 Entwurf, 1 abgelaufen)
-- Diverse Angebotspositionen mit realistischen IT-Dienstleistungen
