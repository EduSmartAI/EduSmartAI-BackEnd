using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;

namespace AiService.Application.Handler.AiChatBot.Command
{
    public class GetChatDetailLearningPathHandler(
        IChatBotLearningPathService _chatBotLearningPathService,
        IIdentityService _identityService
    ) : IRequestHandler<GetChatDetailLearningPathRequest, GetChatDetailLearningPathResponse>
    {
        public async Task<GetChatDetailLearningPathResponse> Handle(GetChatDetailLearningPathRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = _identityService.GetCurrentUser();
                if (currentUser is null)
                {
                    return new GetChatDetailLearningPathResponse
                    {
                        Success = false,
                        Message = "Bạn cần đăng nhập để xem chi tiết chat.",
                        Response = new ChatDetailDto()
                    };
                }

                var userId = currentUser.UserId;
                var result = await _chatBotLearningPathService.GetChatDetailAsync(request.SessionId, userId, cancellationToken);

                if (result == null)
                {
                    return new GetChatDetailLearningPathResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy đoạn chat này hoặc bạn không có quyền truy cập.",
                        Response = new ChatDetailDto()
                    };
                }

                return new GetChatDetailLearningPathResponse
                {
                    Success = true,
                    Response = result
                };
            }
            catch (Exception ex)
            {
                return new GetChatDetailLearningPathResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy chi tiết chat.",
                    Response = new DTOs.ChatDetailDto()
                };
            }
        }
    }
}

