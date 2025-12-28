using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiChatBot.Command
{
    public class AIChatBotLearningPathHandler(IChatBotLearningPathService _chatBotLearningPathService) : IRequestHandler<AIChatBotLearningPathRequest, AIChatBotLearningPathResponse>
    {
        public async Task<AIChatBotLearningPathResponse> Handle(AIChatBotLearningPathRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _chatBotLearningPathService.ChatAsync(request, cancellationToken);
                return new AIChatBotLearningPathResponse
                {
                    Response = result,
                };
            }
            catch (Exception ex)
            {
                var error = new ChatResponseDto
                {
                    Reply = "Mình gặp lỗi khi đọc yêu cầu cho công cụ. Bạn chọn giúp: **AI tạo câu hỏi** hay **gợi ý link bên ngoài**?",
                    RawFinishReason = "BadToolArgs"
                };
                return new AIChatBotLearningPathResponse
                {
                    Response = error,
                };
            }
        }
    }
}
