using BaseService.Application.Interfaces.Repositories;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Infrastructure.Implements;

public class TavilyConfigService : ITavilyConfigService
{
    private readonly ICommandRepository<Systemconfig> _repository;

    public TavilyConfigService(ICommandRepository<Systemconfig> repository)
    {
        _repository = repository;
    }

    public async Task<string?> GetConfigValueByIdAsync(string configId, CancellationToken cancellationToken = default)
    {
        var config = await _repository.FirstOrDefaultAsync(
            predicate: x => x.Id == configId && x.IsActive == true,
            cancellationToken: cancellationToken
        );

        return config?.Value;
    }
}

