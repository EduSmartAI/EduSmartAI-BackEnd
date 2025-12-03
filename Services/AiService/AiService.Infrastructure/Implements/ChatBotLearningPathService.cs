using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using AiService.Domain;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using OpenAI.Chat;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure.Implements
{
    public class ChatBotLearningPathService(
        ChatClient chatClient,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<AiChatLearningPathCollection> lpQuery,
        IRequestClient<GetAllLearningPath> getAllLearningPathClient,
        IRequestClient<GetLearningPathInfo> getLearningPathInfoClient,
        IRequestClient<AiUpdateCourseStatusToSkipped> updateCourseStatusToSkippedClient
    ) : IChatBotLearningPathService
    {
        private const string SystemMessage =
            "You are EduSmart Learning Path Advisor for Information Technology. " +
            "Default language: Vietnamese (vi). " +
            "Your tasks: (1) understand the learner's current level, goals and constraints, " +
            "(2) design a clear step-by-step learning path grouped by phases, " +
            "(3) for each phase, specify key skills and short outcomes, " +
            "(4) when possible, map to concrete course names/codes mentioned by the user. " +
            "You have three tools: `get_user_learning_paths` (list all saved paths), `get_user_learning_path_detail` (detail for a path by ID), and `skip_learning_path_subject` (mark a subject as skipped inside a learning path). " +
            "Whenever you list learning paths, show each entry with its full name, exact GUID, created date, and status label derived from the provided status code (0: Đang tạo, 1: Đang chọn chuyên ngành, 2: Đang học, 3: Đã hoàn thành, 4: Đã đóng, 5: Tạm dừng). " +
            "Always call the appropriate tool instead of guessing any learner data. " +
            "Always answer in concise Vietnamese Markdown with headings (###) and bullet lists. " +
            "Do not return JSON, only natural language answer for the learner.";

        private static readonly JsonSerializerOptions ToolSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly ChatTool GetLearningPathsTool = ChatTool.CreateFunctionTool(
            functionName: "get_user_learning_paths",
            functionDescription:
                "Fetch the learner's saved learning paths. Call this when the user asks to list, count, or browse their learning paths.",
            functionParameters: BinaryData.FromString("""
            {
              "type":"object",
              "properties":{},
              "additionalProperties":false
            }
            """));

        private static readonly ChatTool GetLearningPathDetailTool = ChatTool.CreateFunctionTool(
            functionName: "get_user_learning_path_detail",
            functionDescription:
                "Fetch detailed information of one learning path owned by the current learner. Call this when an ID or specific path is referenced.",
            functionParameters: BinaryData.FromString("""
            {
              "type":"object",
              "properties":{
                "learning_path_id":{
                  "type":"string",
                  "description":"GUID of the learning path the learner wants to inspect"
                }
              },
              "required":["learning_path_id"],
              "additionalProperties":false
            }
            """));

        private static readonly ChatTool SkipLearningPathSubjectTool = ChatTool.CreateFunctionTool(
            functionName: "skip_learning_path_subject",
            functionDescription:
                "Mark a subject inside the learner's specified learning path as skipped. Use this when the learner explicitly asks to skip/ignore a subject.",
            functionParameters: BinaryData.FromString("""
            {
              "type":"object",
              "properties":{
                "learning_path_id":{
                  "type":"string",
                  "description":"GUID of the learning path containing the subject"
                },
                "subject_code":{
                  "type":"string",
                  "description":"Subject code (case-insensitive) the learner wants to skip"
                }
              },
              "required":["learning_path_id","subject_code"],
              "additionalProperties":false
            }
            """));


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
                var (replyRaw, finishReason) = await RunLearningPathAsync(messages, userId, email, ct);
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
                    RawFinishReason = finishReason
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
            Guid userId,
            string? email,
            CancellationToken ct)
        {
            var options = new ChatCompletionOptions
            {
                Tools = { GetLearningPathsTool, GetLearningPathDetailTool, SkipLearningPathSubjectTool }
            };

            while (true)
            {
                var res = await chatClient.CompleteChatAsync(messages, options, ct);

                if (res.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messages.Add(new AssistantChatMessage(res));

                    foreach (var call in res.Value.ToolCalls)
                    {
                        var toolPayload = await HandleToolCallAsync(call, userId, email, ct);
                        messages.Add(new ToolChatMessage(call.Id, toolPayload));
                    }

                    continue;
                }

                if (res.Value.Content.Count == 0)
                {
                    return (string.Empty, res.Value.FinishReason.ToString());
                }

                var text = res.Value.Content[0].Text.ToString();
                var reason = res.Value.FinishReason.ToString();

                return (text, reason);
            }
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

        private async Task<string> HandleToolCallAsync(
            ChatToolCall call,
            Guid userId,
            string? email,
            CancellationToken ct)
        {
            try
            {
                var argsJson = call.FunctionArguments?.ToString();
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
                var root = doc.RootElement;

                switch (call.FunctionName)
                {
                    case "get_user_learning_paths":
                        {
                            var response = await getAllLearningPathClient
                                .GetResponse<GetAllLearningPathResponse>(new GetAllLearningPath(userId), ct);

                            var payload = new
                            {
                                success = response.Message.Success,
                                message = response.Message.Message,
                                learningPaths = (response.Message.Response ?? new List<AiLearningPathSummaryDto>())
                                    .Select(lp => new
                                    {
                                        id = lp.PathId,
                                        name = lp.PathName,
                                        statusCode = lp.Status,
                                        status = MapLearningPathStatus(lp.Status),
                                        createdAt = lp.CreatedAt
                                    })
                            };

                            return JsonSerializer.Serialize(payload, ToolSerializerOptions);
                        }

                    case "get_user_learning_path_detail":
                        {
                            if (!root.TryGetProperty("learning_path_id", out var idProp) ||
                                !Guid.TryParse(idProp.GetString(), out var learningPathId))
                            {
                                return JsonSerializer.Serialize(new
                                {
                                    success = false,
                                    message = "learning_path_id is required and must be a valid GUID"
                                }, ToolSerializerOptions);
                            }

                            var response = await getLearningPathInfoClient
                                .GetResponse<GetLearningPathInfoResponse>(new GetLearningPathInfo(userId, learningPathId), ct);

                            var detail = response.Message.Response;

                            var payload = new
                            {
                                success = response.Message.Success,
                                message = response.Message.Message,
                                    learningPath = detail == null ? null : new
                                    {
                                        id = detail.PathId,
                                        name = detail.PathName,
                                        statusCode = detail.Status,
                                        status = MapLearningPathStatus(detail.Status),
                                        completionPercent = detail.CompletionPercent,
                                    basicCourseGroups = detail.BasicCourseGroups?
                                        .Select(group => new
                                        {
                                            subjectCode = group.SubjectCode,
                                            status = group.Status,
                                            courses = group.Courses?
                                                .Select(course => new
                                                {
                                                    courseId = course.CourseId,
                                                    title = course.Title,
                                                    subjectCode = course.SubjectCode,
                                                    status = course.Status,
                                                    semesterPosition = course.SemesterPosition,
                                                    provider = course.Provider
                                                })
                                        }),
                                    internalMajors = detail.InternalMajors?
                                        .Select(major => new
                                        {
                                            majorId = major.MajorId,
                                            majorCode = major.MajorCode,
                                            reason = major.Reason,
                                            positionIndex = major.PositionIndex,
                                            courseGroups = major.CourseGroups?
                                                .Select(group => new
                                                {
                                                    subjectCode = group.SubjectCode,
                                                    status = group.Status,
                                                    courses = group.Courses?
                                                        .Select(course => new
                                                        {
                                                            courseId = course.CourseId,
                                                            title = course.Title,
                                                            subjectCode = course.SubjectCode,
                                                            status = course.Status,
                                                            semesterPosition = course.SemesterPosition,
                                                            provider = course.Provider
                                                        })
                                                })
                                        })
                                }
                            };

                            return JsonSerializer.Serialize(payload, ToolSerializerOptions);
                        }

                    case "skip_learning_path_subject":
                        {
                            if (!root.TryGetProperty("learning_path_id", out var lpProp) ||
                                !Guid.TryParse(lpProp.GetString(), out var learningPathId))
                            {
                                return JsonSerializer.Serialize(new
                                {
                                    success = false,
                                    message = "learning_path_id is required and must be a valid GUID"
                                }, ToolSerializerOptions);
                            }

                            if (!root.TryGetProperty("subject_code", out var subjectProp) ||
                                string.IsNullOrWhiteSpace(subjectProp.GetString()))
                            {
                                return JsonSerializer.Serialize(new
                                {
                                    success = false,
                                    message = "subject_code is required"
                                }, ToolSerializerOptions);
                            }

                            if (string.IsNullOrWhiteSpace(email))
                            {
                                return JsonSerializer.Serialize(new
                                {
                                    success = false,
                                    message = "Không thể xác định email người dùng để cập nhật lộ trình."
                                }, ToolSerializerOptions);
                            }

                            var subjectCode = subjectProp.GetString()!.Trim();
                            var evt = new AiUpdateCourseStatusToSkipped(userId, email, learningPathId, subjectCode);

                            var response = await updateCourseStatusToSkippedClient
                                .GetResponse<AiUpdateCourseStatusToSkippedResponse>(evt, ct);

                            var payload = new
                            {
                                success = response.Message.Success,
                                message = response.Message.Message,
                                detail = response.Message.Response
                            };

                            return JsonSerializer.Serialize(payload, ToolSerializerOptions);
                        }

                    default:
                        return JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = $"Unknown tool: {call.FunctionName}"
                        }, ToolSerializerOptions);
                }
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "tool_execution_failed",
                    detail = ex.Message
                }, ToolSerializerOptions);
            }
        }

        private static string MapLearningPathStatus(short status)
        {
            return status switch
            {
                (short)ConstantEnum.LearningPathStatus.Generating => "Đang tạo",
                (short)ConstantEnum.LearningPathStatus.Choosing => "Đang chọn chuyên ngành",
                (short)ConstantEnum.LearningPathStatus.InProgress => "Đang học",
                (short)ConstantEnum.LearningPathStatus.Completed => "Đã hoàn thành",
                (short)ConstantEnum.LearningPathStatus.Closed => "Đã đóng",
                (short)ConstantEnum.LearningPathStatus.Paused => "Tạm dừng",
                _ => "Không xác định"
            };
        }
    }
}
