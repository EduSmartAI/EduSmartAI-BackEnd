using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<SearchHit>> SearchTopKAsync(float[] queryEmbedding, int k, CancellationToken ct);
        Task<IReadOnlyList<DocumentDto>> SearchCoursesTopKAsync(float[] queryEmbedding, string needle, int k, CancellationToken ct);
    }
}
