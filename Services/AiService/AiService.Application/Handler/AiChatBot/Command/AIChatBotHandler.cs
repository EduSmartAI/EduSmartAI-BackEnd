using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiChatBot.Command
{
    public class AIChatBotHandler(IChatBotService _chatBotService) : IRequestHandler<AIChatBotRequest, AIChatBotResponse>
    {
        public async Task<AIChatBotResponse> Handle(AIChatBotRequest request, CancellationToken cancellationToken)
        {
            var result = await _chatBotService.ChatAsync(request, cancellationToken);
            return new AIChatBotResponse
            {
                Response = result,
            };
        }
    }
}
