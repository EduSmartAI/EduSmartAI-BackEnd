using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using AiService.Domain;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using OpenAI.Chat;

namespace AiService.Infrastructure.Implements
{
    public class ChatBotLearningPathService(
        ChatClient chatClient,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<AiChatLearningPathCollection> lpQuery
    ) : IChatBotLearningPathService
    {
        private const string SystemMessage =
            "You are EduSmart Learning Path Advisor for Information Technology. " +
            "Default language: Vietnamese (vi). " +
            "Your tasks: (1) understand the learner's current level, goals and constraints, " +
            "(2) design a clear step-by-step learning path grouped by phases, " +
            "(3) for each phase, specify key skills and short outcomes, " +
            "(4) when possible, map to concrete course names/codes mentioned by the user. " +
            "Always answer in concise Vietnamese Markdown with headings (###) and bullet lists. " +
            "Do not return JSON, only natural language answer for the learner.";

        public async Task<ChatResponseDto> ChatAsync(
            AIChatBotLearningPathRequest req,
            CancellationToken ct = default)
        {
            try
            {
                var currentUser = identityService.GetCurrentUser();
                if (currentUser is null)
                {
                    return new ChatResponseDto
                    {
                        Reply = "Bạn cần đăng nhập để mình có thể lưu và quản lý lộ trình học cho riêng bạn.",
                        RawFinishReason = "Unauthenticated"
                    };
                }

                if (string.IsNullOrWhiteSpace(req.Request.Message))
                {
                    return new ChatResponseDto
                    {
                        Reply = "Bạn hãy nhập nội dung câu hỏi hoặc mục tiêu học để mình tư vấn lộ trình nhé.",
                        RawFinishReason = "EmptyMessage"
                    };
                }

                var userId = currentUser.UserId;
                var email = currentUser.Email;

                // 1) Lấy đúng session (đoạn chat) hoặc tạo mới
                var sessionDoc = await LoadOrCreateSessionAsync(
                    sessionId: req.Request.SessionId,
                    userId: userId,
                    email: email);

                // 2) Append câu hỏi mới của user vào lịch sử
                AppendUserMessage(sessionDoc, req.Request.Message);

                // 3) Build full history gửi cho AI
                var messages = BuildChatMessagesFromSession(sessionDoc);

                // 4) Gọi AI, lấy text markdown
                var (replyRaw, finishReason) = await RunLearningPathAsync(messages, ct);
                var reply = NormalizeMarkdown(replyRaw);

                // 5) Nếu có trả lời thì thêm vào lịch sử như assistant
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    sessionDoc.Messages.Add(new ChatHistoryLearningPathItem
                    {
                        Role = "assistant",
                        Content = reply,
                        RawFinishReason = finishReason
                    });
                }

                // Audit + lưu lịch sử (kể cả khi AI không trả lời gì thì vẫn lưu message của user)
                sessionDoc.UpdatedAt = DateTime.UtcNow;
                sessionDoc.UpdatedBy = email;

                unitOfWork.Store(sessionDoc);
                await unitOfWork.SessionSaveChangesAsync();

                if (string.IsNullOrWhiteSpace(reply))
                {
                    return new ChatResponseDto
                    {
                        Reply = "Mình chưa nhận được phản hồi từ AI. Bạn thử nhắn lại giúp mình nhé.",
                        RawFinishReason = "EmptyResult"
                    };
                }

                return new ChatResponseDto
                {
                    Reply = reply,
                    RawFinishReason = "Stop"
                };
            }
            catch
            {
                return new ChatResponseDto
                {
                    Reply = "Mình gặp lỗi khi tạo lộ trình học cho bạn. Bạn thử diễn đạt lại hoặc nhắn lại sau một chút nhé.",
                    RawFinishReason = "Error"
                };
            }
        }

        private async Task<AiChatLearningPathCollection> LoadOrCreateSessionAsync(
            Guid? sessionId,
            Guid userId,
            string? email)
        {
            AiChatLearningPathCollection? sessionDoc = null;

            if (sessionId is Guid sid)
            {
                // Lấy thẳng từ Marten, không dùng cache
                sessionDoc = await lpQuery.FirstOrDefaultAsync(
                    x => x.Id == sid && x.UserId == userId && x.IsActive);

                if (sessionDoc is null)
                {
                    sessionDoc = CreateNewSessionDocument(sid, userId, email);
                }
            }
            else
            {
                sessionDoc = CreateNewSessionDocument(Guid.NewGuid(), userId, email);
            }

            return sessionDoc;
        }

        private static AiChatLearningPathCollection CreateNewSessionDocument(
            Guid id,
            Guid userId,
            string? email)
        {
            return new AiChatLearningPathCollection
            {
                Id = id,
                UserId = userId,
                Name = "Lộ trình học mới",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = email,
                UpdatedBy = email,
                IsActive = true,
                Messages = new List<ChatHistoryLearningPathItem>()
            };
        }

        private static void AppendUserMessage(AiChatLearningPathCollection sessionDoc, string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            sessionDoc.Messages ??= new List<ChatHistoryLearningPathItem>();

            sessionDoc.Messages.Add(new ChatHistoryLearningPathItem
            {
                Role = "user",
                Content = message,
                RawFinishReason = null
            });
        }

        private static List<ChatMessage> BuildChatMessagesFromSession(AiChatLearningPathCollection sessionDoc)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemMessage)
            };

            if (sessionDoc.Messages is { Count: > 0 })
            {
                foreach (var item in sessionDoc.Messages)
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) continue;

                    var role = item.Role?.Trim().ToLowerInvariant();

                    messages.Add(role switch
                    {
                        "assistant" => new AssistantChatMessage(item.Content),
                        "system" => new SystemChatMessage(item.Content),
                        _ => new UserChatMessage(item.Content)
                    });
                }
            }

            return messages;
        }

        private async Task<(string Text, string RawFinishReason)> RunLearningPathAsync(
            List<ChatMessage> messages,
            CancellationToken ct)
        {
            var res = await chatClient.CompleteChatAsync(
                messages,
                options: null,
                cancellationToken: ct);

            if (res.Value.Content.Count == 0)
                return (string.Empty, res.Value.FinishReason.ToString());

            var text = res.Value.Content[0].Text.ToString();
            var reason = res.Value.FinishReason.ToString();

            return (text, reason);
        }

        private static string NormalizeMarkdown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            s = s.Replace("\r\n", "\n");

            while (s.Contains("\n\n\n", StringComparison.Ordinal))
            {
                s = s.Replace("\n\n\n", "\n\n");
            }

            return s.Trim();
        }

        public async Task<List<ChatSummaryDto>> GetAllChatsAsync(Guid userId, CancellationToken ct = default)
        {
            var allChats = await lpQuery.ToListAsync(
                x => x.UserId == userId && x.IsActive);

            var chats = allChats
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new ChatSummaryDto
                {
                    Id = x.Id,
                    Name = x.Name ?? "Lộ trình học mới",
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    MessageCount = x.Messages != null ? x.Messages.Count : 0
                })
                .ToList();

            return chats;
        }

        public async Task<ChatDetailDto?> GetChatDetailAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
        {
            var sessionDoc = await lpQuery.FirstOrDefaultAsync(
                x => x.Id == sessionId && x.UserId == userId && x.IsActive);

            if (sessionDoc == null)
            {
                return null;
            }

            var messages = sessionDoc.Messages?
                .Select(m => new ChatHistoryLearningPathItemDto
                {
                    Role = m.Role,
                    Content = m.Content,
                    RawFinishReason = m.RawFinishReason
                })
                .ToList() ?? new List<ChatHistoryLearningPathItemDto>();

            return new ChatDetailDto
            {
                Id = sessionDoc.Id,
                Name = sessionDoc.Name,
                Messages = messages,
                CreatedAt = sessionDoc.CreatedAt,
                UpdatedAt = sessionDoc.UpdatedAt,
                CreatedBy = sessionDoc.CreatedBy,
                UpdatedBy = sessionDoc.UpdatedBy
            };
        }
    }
}
