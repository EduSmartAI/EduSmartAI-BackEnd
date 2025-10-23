using AiService.Application.Features.AiSearch;
using AiService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AiService.Application.Handler.AiSearch.Command
{
    public class AiSearchChatBotHandler(IAISearchService _aiSearchService) : ICommandHandler<AiSearchChatBotRequest, AiSearchResponse>
    {
        public async Task<AiSearchResponse> Handle(AiSearchChatBotRequest request, CancellationToken cancellationToken)
        {
            var res = await _aiSearchService.FindMultipleChoiceExcercises(request.topic, request.difficultyLevel, true);
            return new AiSearchResponse
            {
                Success = true,
                Response = res
            };
        }
    }
}
