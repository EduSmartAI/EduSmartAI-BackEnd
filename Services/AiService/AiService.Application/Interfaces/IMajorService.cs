using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Interfaces
{
    public interface IMajorService
    {
        Task<List<Major>> LoadMajorsAsync(CancellationToken ct);
    }
}
