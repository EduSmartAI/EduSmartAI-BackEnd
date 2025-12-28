using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;

namespace AiService.Application.Interfaces
{
    public interface IChatBotService
    {
        Task<ChatResponseDto> ChatAsync(AIChatBotRequest req, CancellationToken ct = default);
    }
}
