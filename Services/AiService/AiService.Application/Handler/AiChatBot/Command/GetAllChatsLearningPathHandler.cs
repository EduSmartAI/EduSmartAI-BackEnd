using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;

namespace AiService.Application.Handler.AiChatBot.Command
{
    public class GetAllChatsLearningPathHandler(
        IChatBotLearningPathService _chatBotLearningPathService,
        IIdentityService _identityService
    ) : IRequestHandler<GetAllChatsLearningPathRequest, GetAllChatsLearningPathResponse>
    {
        public async Task<GetAllChatsLearningPathResponse> Handle(GetAllChatsLearningPathRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = _identityService.GetCurrentUser();
                if (currentUser is null)
                {
                    return new GetAllChatsLearningPathResponse
                    {
                        Success = false,
                        Message = "Bạn cần đăng nhập để xem lịch sử chat.",
                        Response = new List<ChatSummaryDto>()
                    };
                }

                var userId = currentUser.UserId;
                var result = await _chatBotLearningPathService.GetAllChatsAsync(userId, cancellationToken);

                return new GetAllChatsLearningPathResponse
                {
                    Success = true,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new GetAllChatsLearningPathResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách chat.",
                    Response = new List<DTOs.ChatSummaryDto>()
                };
            }
        }
    }
}

