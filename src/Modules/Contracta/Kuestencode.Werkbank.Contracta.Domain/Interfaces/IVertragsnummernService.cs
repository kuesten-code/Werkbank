namespace Kuestencode.Werkbank.Contracta.Domain.Interfaces;

public interface IVertragsnummernService
{
    Task<string> NaechsteNummerAsync();
}
