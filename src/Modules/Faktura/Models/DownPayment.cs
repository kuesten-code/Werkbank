using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuestencode.Faktura.Models;

public class DownPayment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    [Required(ErrorMessage = "Beschreibung ist erforderlich")]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal NetAmount { get; set; }

    [Range(0, 100, ErrorMessage = "MwSt-Satz muss zwischen 0 und 100 liegen")]
    public decimal VatRate { get; set; }

    // Bruttobetrag - wird vom Rechnungsbrutto abgezogen
    [Required(ErrorMessage = "Betrag ist erforderlich")]
    [Range(0.01, 999999.99, ErrorMessage = "Betrag muss größer als 0 sein")]
    public decimal Amount { get; set; }

    [NotMapped]
    public decimal VatAmount => Amount - NetAmount;

    public DateTime? PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SourceInvoiceId { get; set; }

    // Abschlagsrechnung, auf die sich dieser Abschlag bezieht - wird separat geladen
    [NotMapped]
    public Invoice? SourceInvoice { get; set; }

    public void ApplyNetAmount(decimal netAmount)
    {
        NetAmount = netAmount;
        Amount = Math.Round(netAmount * (1 + VatRate / 100), 2, MidpointRounding.AwayFromZero);
    }

    public void ApplyGrossAmount(decimal grossAmount)
    {
        Amount = grossAmount;
        NetAmount = Math.Round(grossAmount / (1 + VatRate / 100), 2, MidpointRounding.AwayFromZero);
    }

    // Netto bleibt fix, weil sich ein Abschlag auf ein vereinbartes Entgelt bezieht
    public void ApplyVatRate(decimal vatRate)
    {
        VatRate = vatRate;
        ApplyNetAmount(NetAmount);
    }

    // Bei gemischten Steuersätzen der Abschlagsrechnung ergibt sich ein effektiver Durchschnittssatz
    public void ApplySourceInvoice(Invoice sourceInvoice)
    {
        NetAmount = Math.Round(sourceInvoice.TotalNetAfterDiscount, 2, MidpointRounding.AwayFromZero);
        Amount = Math.Round(sourceInvoice.TotalGross, 2, MidpointRounding.AwayFromZero);
        VatRate = sourceInvoice.TotalNetAfterDiscount == 0
            ? 0
            : Math.Round(sourceInvoice.TotalVat / sourceInvoice.TotalNetAfterDiscount * 100, 2, MidpointRounding.AwayFromZero);
    }
}
