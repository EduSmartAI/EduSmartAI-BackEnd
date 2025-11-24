using AiService.Application.DTOs;
using MediatR;

namespace AiService.Application.Features.AIChatBot
{
    public record AIChatBotLearningPathRequest(ChatBotLearningPathRequestDto Request) : IRequest<AIChatBotLearningPathResponse>
    {
    }
}

