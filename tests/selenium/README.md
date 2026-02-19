# Selenium Tests

Diese Tests liegen als eigenständiges xUnit-Projekt in `tests/selenium`.

## Voraussetzungen

- Chrome ist installiert
- Werkbank läuft lokal (oder erreichbar über `SELENIUM_BASE_URL`)

## Environment Variablen

- `SELENIUM_BASE_URL` (optional, default `http://localhost:8080`)
- `SELENIUM_HEADLESS` (optional, default `true`)
- `SELENIUM_ADMIN_EMAIL`
- `SELENIUM_ADMIN_PASSWORD`
- `SELENIUM_BUERO_EMAIL`
- `SELENIUM_BUERO_PASSWORD`
- `SELENIUM_MITARBEITER_EMAIL`
- `SELENIUM_MITARBEITER_PASSWORD`

## Ausführen

```powershell
# nur Selenium-Projekt
 dotnet test tests/selenium/Kuestencode.SeleniumTests.csproj

# oder über Solution-Filter
 dotnet test Kuestencode.Werkbank.sln --filter FullyQualifiedName~Kuestencode.SeleniumTests
```

## Enthaltene Tests

- `LoginPageTests.LoginPage_ShowsEmailFieldAndSubmitButton`
- `RoleNavigationTests.Admin_SeesSettingsAndTeam`
- `RoleNavigationTests.Buero_DoesNotSeeSettingsOrTeam`
- `RoleNavigationTests.Mitarbeiter_SeesOnlyRapportArea`
- `HostPagesSmokeTests.HostRoute_Unauthenticated_DoesNotCrash`
- `HostPagesSmokeTests.HostRoute_AsAdmin_DoesNotCrash`
- `FakturaPagesSmokeTests.FakturaRoute_Unauthenticated_RedirectsToLogin_AndDoesNotCrash`
- `FakturaPagesSmokeTests.FakturaRoute_AsAdmin_DoesNotCrash`

## Abgedeckte Host-Seiten (Smoke)

- `/`
- `/login`
- `/forgot-password`
- `/invite/{Token}`
- `/reset/{Token}`
- `/setup`
- `/customers`
- `/customers/create`
- `/customers/edit/{id}`
- `/settings/auth`
- `/settings/company`
- `/settings/email`
- `/team-members`
- `/team-members/create`
- `/team-members/edit/{id}`
- `/m/{Token}`
- `/m/{Token}/rapport`

## Abgedeckte Faktura-Seiten (Smoke)

- `/faktura`
- `/faktura/invoices`
- `/faktura/invoices/create`
- `/faktura/invoices/edit/{id}`
- `/faktura/invoices/details/{id}`
- `/faktura/settings/email-anpassung`
- `/faktura/settings/pdf-anpassung`
