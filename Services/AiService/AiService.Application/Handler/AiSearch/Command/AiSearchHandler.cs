using AiService.Application.Features.AiSearch;
using AiService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AiService.Application.Handler.AiSearch.Command
{
    public class AiSearchHandler(IAISearchService _aiSearchService) : ICommandHandler<AiSearchRequest, AiSearchResponse>
    {
        public async Task<AiSearchResponse> Handle(AiSearchRequest request, CancellationToken cancellationToken)
        {
            var res = await _aiSearchService.FindCourseResourcesAsync(request.topic, "", true);
            return new AiSearchResponse
            {
                Success = true,
                Response = res
            };
        }
    }
}
