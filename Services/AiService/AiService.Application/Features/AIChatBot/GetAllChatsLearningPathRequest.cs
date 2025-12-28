using MediatR;

namespace AiService.Application.Features.AIChatBot
{
    public record GetAllChatsLearningPathRequest : IRequest<GetAllChatsLearningPathResponse>
    {
    }
}

