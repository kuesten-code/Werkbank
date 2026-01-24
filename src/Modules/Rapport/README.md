# Kuestencode.Rapport

Rapport module for the Kuestencode platform.

## Installation (Microservice)

```bash
cd src/Modules/Rapport
dotnet run
```

## Dependencies

- Kuestencode.Core
- Kuestencode.Shared.UI
- Entity Framework Core (PostgreSQL)
- MudBlazor

## Architecture

```
Kuestencode.Rapport/
+-- Data/
¦   +-- RapportDbContext.cs
¦   +-- Repositories/
+-- Models/
+-- Services/
+-- Pages/
+-- Shared/
+-- RapportModule.cs      # Service registration
+-- ProgramApi.cs         # Service entry point (Microservice mode)
```
