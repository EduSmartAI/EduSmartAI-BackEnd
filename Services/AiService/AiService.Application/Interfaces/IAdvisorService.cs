using AiService.Application.Features.AiEvaluate;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Interfaces
{
    public interface IAdvisorService
    {
        Task<EvaluateResult> EvaluateAsync(AiEvaluateRequest req, CancellationToken ct);
        Task<AskResponse> AskAsync(string question, int k, bool showSources, CancellationToken ct);
    }
}
