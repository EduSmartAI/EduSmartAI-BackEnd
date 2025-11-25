using MediatR;

namespace AiService.Application.Features.AIChatBot
{
    public record GetChatDetailLearningPathRequest(Guid SessionId) : IRequest<GetChatDetailLearningPathResponse>
    {
    }
}

