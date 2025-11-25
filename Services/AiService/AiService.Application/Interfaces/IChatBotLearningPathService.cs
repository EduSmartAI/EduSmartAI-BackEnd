using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;

namespace AiService.Application.Interfaces
{
    public interface IChatBotLearningPathService
    {
        Task<ChatResponseDto> ChatAsync(AIChatBotLearningPathRequest req, CancellationToken ct = default);
        Task<List<ChatSummaryDto>> GetAllChatsAsync(Guid userId, CancellationToken ct = default);
        Task<ChatDetailDto?> GetChatDetailAsync(Guid sessionId, Guid userId, CancellationToken ct = default);
    }
}
