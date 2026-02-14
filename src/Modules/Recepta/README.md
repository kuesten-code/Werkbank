# Kuestencode.Recepta

Microservice für Eingangsrechnungsverwaltung in der Küstencode Werkbank.

## Features

- Eingangsrechnungen erfassen, bearbeiten und verwalten
- Statusworkflow (Entwurf → Gebucht → Bezahlt)
- XRechnung/ZUGFeRD-Import (XML direkt, ZUGFeRD-PDF mit eingebettetem XML)
- OCR-Texterkennung (Tesseract) für Bild- und PDF-Dokumente
- Selbstlernender Musterabgleich pro Lieferant (Pattern-Learning)
- 3-Phasen-Scan-Workflow: Upload → Analyse → Formular mit Pre-Fill
- Lieferantenverwaltung mit USt-ID, IBAN, BIC
- Automatisches Lieferanten-Matching: USt-ID → IBAN → Name
- Dateianhänge mit Vorschau (PDF, JPG, PNG)
- Kategorisierung (Material, Fremdleistung, Büro, Reise, Sonstig)
- Fälligkeitsdatum-Tracking mit Überfällig-Anzeige
- Cross-Modul-Integration mit Acta (Belege auf Projekte buchen, Kostenauswertung)

## Beleg-Statusübergänge

```
┌──────────────┐
│    Draft     │
│  (Entwurf)   │
└──────┬───────┘
       │ Buchen
       ▼
┌──────────────┐
│    Booked    │◄────────┐
│  (Gebucht)   │         │
└──────┬───────┘         │
       │ Bezahlen   Zurücksetzen
       ▼                 │
┌──────────────┐         │
│     Paid     │         │
│  (Bezahlt)   │         │
└──────────────┘         │
                         │
  Booked ───────────────►Draft
```

**Bearbeitbar** in: Draft
**Valide Übergänge**: Draft → Booked, Booked → Paid, Booked → Draft

## Installation (Microservice)

```bash
cd src/Modules/Recepta/Kuestencode.Werkbank.Recepta
dotnet run
```

Erreichbar unter `http://localhost:8085` (direkt) bzw. `http://localhost:8080/recepta` (über Host-Proxy).

## Konfiguration

| Variable | Beschreibung |
|----------|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL-Verbindungsstring |
| `ServiceUrls:Host` | URL des Host-Services |
| `ServiceUrls:Self` | Eigene URL für Modul-Registrierung |
| `APPLY_MIGRATIONS` | Automatische DB-Migrationen (`true`/`false`) |

## API Endpoints

### Belege (Documents)

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| `GET` | `/api/recepta/documents` | Alle Belege (Filter: `?status=Draft&category=Material&supplierId=...`) |
| `GET` | `/api/recepta/documents/{id}` | Beleg nach ID |
| `POST` | `/api/recepta/documents` | Neuen Beleg erstellen |
| `POST` | `/api/recepta/documents/scan` | Beleg aus Scan erstellen (XRechnung/OCR) |
| `PUT` | `/api/recepta/documents/{id}` | Beleg aktualisieren |
| `POST` | `/api/recepta/documents/{id}/status` | Status ändern |
| `POST` | `/api/recepta/documents/{id}/learn` | Muster lernen |
| `DELETE` | `/api/recepta/documents/{id}` | Beleg löschen (nur Draft) |
| `GET` | `/api/recepta/documents/next-number` | Nächste Belegnummer generieren |
| `POST` | `/api/recepta/documents/ocr` | Text aus Datei extrahieren |
| `POST` | `/api/recepta/documents/ocr/learn` | OCR-Muster lernen |
| `POST` | `/api/recepta/documents/ocr/extract` | Felder aus OCR-Text extrahieren |
| `GET` | `/api/recepta/documents/project/{projectId}` | Belege eines Projekts |
| `GET` | `/api/recepta/documents/project/{projectId}/expenses` | Projektkosten-Zusammenfassung |

### Lieferanten (Suppliers)

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| `GET` | `/api/recepta/suppliers` | Alle Lieferanten (Filter: `?search=...`) |
| `GET` | `/api/recepta/suppliers/{id}` | Lieferant nach ID |
| `POST` | `/api/recepta/suppliers` | Neuen Lieferanten erstellen |
| `PUT` | `/api/recepta/suppliers/{id}` | Lieferanten aktualisieren |
| `DELETE` | `/api/recepta/suppliers/{id}` | Lieferanten löschen (nur wenn keine Belege) |
| `GET` | `/api/recepta/suppliers/find-by-name` | Lieferant nach Name suchen |
| `GET` | `/api/recepta/suppliers/next-number` | Nächste Lieferantennummer generieren |

### Dateien (Files)

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| `POST` | `/api/recepta/documents/{documentId}/files` | Datei hochladen (mit optionaler OCR) |
| `GET` | `/api/recepta/files/{fileId}` | Datei herunterladen |
| `DELETE` | `/api/recepta/files/{fileId}` | Datei löschen |

## Dependencies

- Kuestencode.Core
- Kuestencode.Shared.UI
- Kuestencode.Shared.Contracts
- Kuestencode.Shared.ApiClients
- Entity Framework Core (PostgreSQL)
- MudBlazor
- ZUGFeRD-csharp (XRechnung/ZUGFeRD-Parsing)
- ZUGFeRD.PDF-csharp (ZUGFeRD-PDF-Extraktion)

## Architektur

```
Kuestencode.Werkbank.Recepta/               # Hauptprojekt (Blazor Server + API)
├── Controllers/
│   ├── DocumentsController.cs              # Beleg-CRUD + Scan + OCR
│   ├── SuppliersController.cs              # Lieferanten-CRUD
│   ├── FilesController.cs                  # Datei-Upload/Download
│   └── Dtos/
│       ├── DocumentApiDtos.cs              # API Request/Response DTOs
│       └── SupplierApiDtos.cs
├── Pages/
│   ├── Index.razor                         # Dashboard (Statistiken)
│   ├── Belege/
│   │   ├── Index.razor                     # Belegliste mit Filtern
│   │   ├── Details.razor                   # Beleg-Detailansicht
│   │   ├── Edit.razor                      # Beleg bearbeiten
│   │   └── Scan.razor                      # 3-Phasen-Scan (Upload → Analyse → Formular)
│   └── Lieferanten/
│       ├── Index.razor                     # Lieferantenliste
│       └── Edit.razor                      # Lieferant erstellen/bearbeiten
├── Services/
│   ├── Interfaces/
│   │   ├── IDocumentService.cs             # Beleg-Service
│   │   ├── IDocumentFileService.cs         # Datei-Service
│   │   ├── ISupplierService.cs             # Lieferanten-Service
│   │   ├── IOcrService.cs                  # OCR-Texterkennung
│   │   ├── IOcrPatternService.cs           # Musterabgleich
│   │   ├── IXRechnungService.cs            # XRechnung/ZUGFeRD-Parser
│   │   └── ICachedProjectService.cs        # Acta-Projekt-Cache
│   └── Implementation/
│       ├── DocumentService.cs              # XRechnung-First-Flow + OCR-Fallback
│       ├── DocumentFileService.cs          # Datei-Speicherung auf Dateisystem
│       ├── SupplierService.cs              # Lieferanten-Logik
│       ├── TesseractOcrService.cs          # Tesseract OCR
│       ├── OcrPatternService.cs            # Pattern-Learning und -Extraktion
│       ├── XRechnungService.cs             # ZUGFeRD-csharp Parser (XML + PDF)
│       ├── CachedProjectService.cs         # 5-Min-Cache für Acta-Projekte
│       ├── ApiCompanyService.cs            # Firmendaten vom Host
│       ├── ApiCustomerService.cs           # Kundendaten vom Host
│       └── ApiModuleRegistry.cs            # Stub für Standalone-Modus
├── Shared/
│   ├── Components/
│   │   └── InlineSupplierCreate.razor      # Inline-Lieferanten-Anlage mit Pre-Fill
│   └── Dialogs/
│       └── ConfirmDialog.razor
├── ReceptaModule.cs                        # Service-Registrierung
└── ProgramApi.cs                           # Entry Point (Microservice)

Kuestencode.Werkbank.Recepta.Domain/        # Domain-Schicht
├── Entities/
│   ├── Document.cs                         # Beleg-Entity (Guid ID, Status, Beträge, ...)
│   ├── DocumentFile.cs                     # Dateianhang-Entity
│   ├── Supplier.cs                         # Lieferant-Entity (USt-ID, IBAN, BIC, ...)
│   └── SupplierOcrPattern.cs               # Gelerntes OCR-Muster pro Lieferant+Feld
├── Enums/
│   ├── DocumentStatus.cs                   # Draft, Booked, Paid
│   └── DocumentCategory.cs                 # Material, Subcontractor, Office, Travel, Other
└── Dtos/
    ├── DocumentDtos.cs                     # Create/Update/Filter/Scan DTOs
    ├── SupplierDtos.cs                     # Create/Update DTOs
    └── XRechnungData.cs                    # Strukturierte XRechnung-Daten + Positionen

Kuestencode.Werkbank.Recepta.Data/          # Datenzugriff
├── ReceptaDbContext.cs                     # DbContext (Schema: "recepta")
├── Repositories/
│   ├── IDocumentRepository.cs / DocumentRepository.cs
│   ├── IDocumentFileRepository.cs / DocumentFileRepository.cs
│   ├── ISupplierRepository.cs / SupplierRepository.cs
│   └── ISupplierOcrPatternRepository.cs / SupplierOcrPatternRepository.cs
└── Migrations/
```

## Datenbank

Schema: `recepta`

### Tabelle: Suppliers

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Lieferanten-ID |
| SupplierNumber | varchar(50, unique) | Lieferantennummer (L-NNNN) |
| Name | varchar(200) | Lieferantenname |
| Address, PostalCode, City | varchar | Adresse (optional) |
| Country | varchar(5) | Ländercode (Standard: DE) |
| Email | varchar(200) | E-Mail (optional) |
| Phone | varchar(50) | Telefon (optional) |
| TaxId | varchar(50) | USt-ID (optional) |
| Iban | varchar(34) | IBAN (optional) |
| Bic | varchar(11) | BIC (optional) |
| Notes | varchar(2000) | Notizen (optional) |
| CreatedAt, UpdatedAt | timestamp | Automatische Zeitstempel |

### Tabelle: Documents

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Beleg-ID |
| DocumentNumber | varchar(50, unique) | Interne Belegnummer |
| SupplierId | uuid (FK) | Lieferant (Restrict Delete) |
| InvoiceNumber | varchar(100) | Rechnungsnummer des Lieferanten |
| InvoiceDate | date | Rechnungsdatum |
| DueDate | date | Fälligkeitsdatum (optional) |
| AmountNet | decimal(18,2) | Nettobetrag |
| TaxRate | decimal(5,2) | MwSt-Satz (%) |
| AmountTax | decimal(18,2) | MwSt-Betrag |
| AmountGross | decimal(18,2) | Bruttobetrag |
| Category | varchar | Kategorie (als String) |
| Status | varchar | Status (als String) |
| ProjectId | uuid | Acta-Projekt (optional) |
| OcrRawText | text | OCR-Rohtext (optional) |
| Notes | varchar(2000) | Notizen (optional) |
| CreatedAt, UpdatedAt | timestamp | Automatische Zeitstempel |

### Tabelle: DocumentFiles

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Datei-ID |
| DocumentId | uuid (FK) | Zugehöriger Beleg (Cascade Delete) |
| FileName | varchar(255) | Dateiname |
| ContentType | varchar(100) | MIME-Typ |
| FileSize | bigint | Dateigröße in Bytes |
| StoragePath | varchar(500) | Pfad im Dateisystem |
| CreatedAt | timestamp | Erstellungszeitpunkt |

### Tabelle: SupplierOcrPatterns

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | uuid (PK) | Eindeutige Pattern-ID |
| SupplierId | uuid (FK) | Lieferant (Cascade Delete) |
| FieldName | varchar(50) | Feldname (InvoiceNumber, AmountGross, ...) |
| Pattern | varchar(500) | Gelerntes Textmuster (Kontext vor dem Wert) |
| CreatedAt, UpdatedAt | timestamp | Automatische Zeitstempel |

## XRechnung/ZUGFeRD-Erkennung

### Erkennungs-Flow

```
Datei hochladen
       │
       ▼
   Extension?
   ┌───┴───┐
   │       │
 .xml    .pdf
   │       │
   ▼       ▼
 Parse   ZUGFeRD-XML    .jpg/.png
 XML     im PDF?         │
   │    ┌──┴──┐          │
   │   Ja    Nein        │
   │    │      │          │
   ▼    ▼      └────┬─────┘
 XRechnung-         │
 Daten              ▼
   │           OCR + Pattern-
   │           Extraktion
   ▼                │
 Lieferant          ▼
 matchen:        Lieferant
 USt-ID →        im Text
 IBAN →          suchen
 Name               │
   │                │
   ▼                ▼
 Formular Pre-Fill
```

### Unterstützte Formate

| Format | Dateityp | Bibliothek |
|--------|----------|-----------|
| XRechnung (UBL 2.1) | `.xml` | ZUGFeRD-csharp |
| XRechnung (CII) | `.xml` | ZUGFeRD-csharp |
| ZUGFeRD 1.x/2.x | `.pdf` (mit XML) | ZUGFeRD.PDF-csharp |
| Factur-X | `.pdf` (mit XML) | ZUGFeRD.PDF-csharp |
| Standard-PDF | `.pdf` | Tesseract OCR |
| Bilder | `.jpg`, `.png` | Tesseract OCR |

## Cross-Modul-Integration

### Acta-Integration

Recepta-Belege können Acta-Projekten zugeordnet werden:

- **Projektzuordnung**: Beim Erfassen oder Bearbeiten eines Belegs kann ein Acta-Projekt ausgewählt werden
- **Projekt-Cache**: Acta-Projekte werden mit 5-Minuten-Cache lokal zwischengespeichert
- **Kostenauswertung**: Der Endpoint `/project/{projectId}/expenses` liefert eine Zusammenfassung aller Projektkosten

### Datenfluss

```
Recepta (Projektliste)    →  Host (Proxy)  →  Acta (External-Endpoint)
Acta (Projektkosten)      →  Host (Proxy)  →  Recepta (Expenses-Endpoint)
```
