namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

public class DocumentActivityLog
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
