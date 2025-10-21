using AiService.Application.DTOs;
using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AIChatBot
{
    public record AIChatBotResponse : AbstractApiResponse<ChatResponseDto>
    {
        public override ChatResponseDto Response { get; set; } = new ChatResponseDto();
    }
}
