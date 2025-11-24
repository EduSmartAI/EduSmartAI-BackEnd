using AiService.Application.DTOs;
using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AIChatBot
{
    public record GetChatDetailLearningPathResponse : AbstractApiResponse<ChatDetailDto>
    {
        public override ChatDetailDto Response { get; set; } = new ChatDetailDto();
    }
}

