namespace Kuestencode.Shared.Contracts.Recepta;

public class MarkDocumentsAttachedRequestDto
{
    public List<Guid> DocumentIds { get; set; } = new();
}
