using Kuestencode.Werkbank.Contracta.Data.Repositories;
using Kuestencode.Werkbank.Contracta.Domain.Interfaces;

namespace Kuestencode.Werkbank.Contracta.Services.Implementation;

public class VertragsnummernService : IVertragsnummernService
{
    private readonly IWartungsvertragRepository _repository;

    public VertragsnummernService(IWartungsvertragRepository repository)
    {
        _repository = repository;
    }

    public Task<string> NaechsteNummerAsync() => _repository.GenerateVertragsnummerAsync();
}
