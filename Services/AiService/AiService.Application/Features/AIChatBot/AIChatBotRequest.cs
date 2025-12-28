using AiService.Application.DTOs;
using MediatR;

namespace AiService.Application.Features.AIChatBot
{
    public record AIChatBotRequest(ChatRequestDto Request) : IRequest<AIChatBotResponse>
    {
    }
}
