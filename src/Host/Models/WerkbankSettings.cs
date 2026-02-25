namespace Kuestencode.Werkbank.Host.Models;

public class WerkbankSettings
{
    public Guid Id { get; set; }
    public string? BaseUrl { get; set; }
    public bool AuthEnabled { get; set; } = false;
}
