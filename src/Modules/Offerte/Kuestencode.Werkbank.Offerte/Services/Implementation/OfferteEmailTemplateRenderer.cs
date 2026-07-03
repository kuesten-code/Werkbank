using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using System.Globalization;

namespace Kuestencode.Werkbank.Offerte.Services.Email;

/// <summary>
/// Rendert den Angebots-Inhalt für Emails (Details-Tabelle).
/// Layout/Farben/Anrede/Grußformel kommen zentral vom Host-EmailEngine.
/// </summary>
public class OfferteEmailTemplateRenderer : IOfferteEmailTemplateRenderer
{
    public string RenderContentHtml(Angebot angebot)
    {
        var culture = new CultureInfo("de-DE");
        var formattedTotal = angebot.Bruttosumme.ToString("C", culture);
        var formattedDate = angebot.Erstelldatum.ToString("dd.MM.yyyy", culture);
        var formattedGueltigBis = angebot.GueltigBis.ToString("dd.MM.yyyy", culture);

        return $"""
            <p>anbei erhalten Sie unser Angebot <strong>{angebot.Angebotsnummer}</strong>.</p>
            <table style="width:100%; border-collapse:collapse; background-color:white; padding:15px; margin:15px 0;">
                <tr><td><strong>Angebotsbetrag:</strong></td><td><strong>{formattedTotal}</strong></td></tr>
                <tr><td><strong>Angebotsnummer:</strong></td><td>{angebot.Angebotsnummer}</td></tr>
                <tr><td><strong>Angebotsdatum:</strong></td><td>{formattedDate}</td></tr>
                <tr><td><strong>Gültig bis:</strong></td><td>{formattedGueltigBis}</td></tr>
            </table>
            <p>Das Angebot finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>
            <p>Bei Fragen stehen wir Ihnen gerne zur Verfügung.</p>
            """;
    }

    public string RenderContentText(Angebot angebot)
    {
        var culture = new CultureInfo("de-DE");
        var formattedTotal = angebot.Bruttosumme.ToString("C", culture);
        var formattedDate = angebot.Erstelldatum.ToString("dd.MM.yyyy", culture);
        var formattedGueltigBis = angebot.GueltigBis.ToString("dd.MM.yyyy", culture);

        return $"""
            anbei erhalten Sie unser Angebot {angebot.Angebotsnummer}.

            ANGEBOTSDETAILS:
            ------------------
            Angebotsbetrag:   {formattedTotal}
            Angebotsnummer:   {angebot.Angebotsnummer}
            Angebotsdatum:    {formattedDate}
            Gültig bis:       {formattedGueltigBis}

            Das Angebot finden Sie im Anhang dieser E-Mail als PDF-Datei.

            Bei Fragen stehen wir Ihnen gerne zur Verfügung.
            """;
    }

    public string? ResolveGreeting(Customer kunde, string? customMessage)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            return customMessage;
        }

        if (!string.IsNullOrWhiteSpace(kunde.Salutation))
        {
            return kunde.Salutation;
        }

        return null;
    }
}
