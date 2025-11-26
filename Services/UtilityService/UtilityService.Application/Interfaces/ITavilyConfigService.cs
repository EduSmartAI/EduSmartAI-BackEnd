namespace UtilityService.Application.Interfaces
{
    public interface ITavilyConfigService
    {
        Task<string?> GetConfigValueByIdAsync(string configId, CancellationToken cancellationToken = default);
    }
}
