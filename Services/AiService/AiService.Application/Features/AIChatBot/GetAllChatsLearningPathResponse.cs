using AiService.Application.DTOs;
using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AIChatBot
{
    public record GetAllChatsLearningPathResponse : AbstractApiResponse<List<ChatSummaryDto>>
    {
        public override List<ChatSummaryDto> Response { get; set; } = new List<ChatSummaryDto>();
    }
}

