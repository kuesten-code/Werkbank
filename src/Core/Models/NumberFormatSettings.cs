using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Core.Models;

/// <summary>
/// Zentrale, firmenweite Konfiguration der Nummernkreise für alle Modul-übergreifenden
/// Belegarten. Singleton-Tabelle (analog zu <see cref="Company"/>), verwaltet vom Host.
/// </summary>
public class NumberFormatSettings
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string InvoiceFormat { get; set; } = "YYYY-XXXX";

    [MaxLength(50)]
    public string QuoteFormat { get; set; } = "ANG-YYYY-XXXXX";

    [MaxLength(50)]
    public string ProjectFormat { get; set; } = "P-YYYY-XXXX";

    [MaxLength(50)]
    public string IncomingInvoiceFormat { get; set; } = "ER-YYYY-XXXX";
}
