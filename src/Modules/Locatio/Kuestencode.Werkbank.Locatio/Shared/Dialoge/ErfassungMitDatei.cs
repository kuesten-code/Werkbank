using Kuestencode.Werkbank.Locatio.Domain.Dtos;

namespace Kuestencode.Werkbank.Locatio.Shared.Dialoge;

public record ErfassungMitDatei<T>(T Eintrag, DateiUpload? Datei);
