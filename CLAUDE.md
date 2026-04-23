# Küstencode Werkbank — Projektkonventionen

## Architektur-Überblick

Blazor Server Monorepo mit mehreren eigenständigen Modulen. Jedes Modul läuft als eigener Docker-Container mit eigenem PostgreSQL-Schema.

```
src/
  Core/                          # Shared models, interfaces (Company, Customer, etc.)
  Kuestencode.Shared.Contracts/  # API-DTOs zwischen Modulen
  Kuestencode.Shared.ApiClients/ # Typed HTTP clients für Modul-zu-Modul-Kommunikation
  Kuestencode.Shared.UI/         # Shared Blazor-Komponenten
  Modules/
    Acta/       # Projektmanagement
    Faktura/    # Rechnungsstellung (Blazor Server)
    Offerte/    # Angebote
    Rapport/    # Stundenzettel
    Recepta/    # Belegverwaltung
    Saldo/      # Finanzen
```

## Modul-Struktur (Referenz: Recepta, Offerte, Acta)

Neue Module folgen diesem Schichtaufbau:

```
Modules/MeinModul/
  Kuestencode.MeinModul.Domain/        # Entities, Enums, DTOs
    Entities/
    Enums/
    Dtos/
  Kuestencode.MeinModul.Data/          # EF Core DbContext, Repositories, Migrations
    Migrations/
    Repositories/
    MeinModulDbContext.cs
    MeinModulDbContextFactory.cs
  Kuestencode.MeinModul/               # API (Controller) + Services
    Controllers/
    Services/
      Interfaces/
      Implementation/
    ProgramApi.cs
    MeinModulModule.cs
```

Jedes Modul hat sein **eigenes PostgreSQL-Schema** (kein schema-übergreifendes JOIN).  
Cross-Modul-Kommunikation erfolgt **ausschließlich über HTTP** via `Kuestencode.Shared.ApiClients`.

## Datenbank-Migrationen — ZWINGEND

Bei jeder neuen Migration MÜSSEN zwei Dateien erstellt werden:

1. `YYYYMMDDHHMMSS_BeschreibenderName.cs` — die eigentliche Migration (Up/Down)
2. `YYYYMMDDHHMMSS_BeschreibenderName.Designer.cs` — der Snapshot mit `[DbContext]`- und `[Migration]`-Attribut

**Ohne `.Designer.cs` erkennt EF Core die Migration nicht und wendet sie nicht an.**

Das Muster für die Designer-Datei:
```csharp
[DbContext(typeof(MeinModulDbContext))]
[Migration("YYYYMMDDHHMMSS_BeschreibenderName")]
partial class BeschreibenderName
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) { ... }
}
```

Außerdem muss `MeinModulDbContextModelSnapshot.cs` nach jeder Migration aktualisiert werden.

Migrationen in diesem Projekt verwenden **raw SQL** statt EF-scaffolding:
```csharp
migrationBuilder.Sql(@"
    ALTER TABLE schema.""Tabelle""
    ADD COLUMN IF NOT EXISTS ""NeuesSpalte"" typ NOT NULL DEFAULT wert;
");
```

## Clean Code

- Keine Kommentare die das WAS erklären — nur das WARUM wenn nicht offensichtlich
- Keine `_var`-Präfixe für ungenutzte Parameter; ungenutzte Elemente vollständig entfernen
- Keine Backwards-Compatibility-Shims für entfernte Funktionalität
- Keine defensive Fehlerbehandlung für Szenarien die nicht eintreten können
- Validierung nur an Systemgrenzen (User-Input, externe APIs)
- Keine Abstraktionen über tatsächlichen Bedarf hinaus

## UI — MudBlazor

- Komponente mit generischem Typ immer `T="..."` angeben wenn `ValueChanged` mit Methodengruppe verwendet wird (sonst CS1503-Fehler)
- `@bind-Value` mit `:after` für Callbacks die Recalculation auslösen

## Steuerlogik (Faktura-Modul)

Dreistufige Priorität für MwSt:
1. `company.IsKleinunternehmer` → §19 UStG, 0 %
2. `invoice.IsReverseCharge` → §13b UStG, 0 %
3. Sonst → 19 %

## Docker / Entwicklungsumgebung

- WSL Docker (kein Docker Desktop)
- PostgreSQL-Volume: `./data/postgres` (bind mount)
- Bei Permission-Problemen: `sudo chown -R 999:999 /mnt/c/Repos/Daddelkiste/Werkbank/data/postgres`
- Alle Module haben `APPLY_MIGRATIONS=true` — Migrationen laufen automatisch beim Container-Start
