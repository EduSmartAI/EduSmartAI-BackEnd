using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiChatBot.Command
{
    public class AIChatBotHandler(IChatBotService _chatBotService) : IRequestHandler<AIChatBotRequest, AIChatBotResponse>
    {
        public async Task<AIChatBotResponse> Handle(AIChatBotRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _chatBotService.ChatAsync(request, cancellationToken);
                return new AIChatBotResponse
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
                return new AIChatBotResponse
                {
                    Response = error,
                };
            }
        }
    }
}
