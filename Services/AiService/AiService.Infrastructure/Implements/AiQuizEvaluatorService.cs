using AiService.Application.DTOs;
using AiService.Application.Handler.Quizzes.Commands;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using OpenAI.Chat;
using static AiService.Infrastructure.Helpers.AiQuizEvaluator.AiQuizEvaluatorCommon;

namespace AiService.Infrastructure.Implements
{
    public sealed class AiQuizEvaluatorService(
        GroqOptionsDto _opt,
        ChatClient chat,
        IPublishEndpoint _publish) : IAiQuizEvaluatorService
    {
        /// <summary>
        /// Call Groq AI to evaluate quiz
        /// Method gọi AI để đánh giá bài quiz
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<QuizEvaluateResponse> EvaluateAsync(QuizEvaluableCreatedEvent evt, CancellationToken ct)
        {
            var response = new QuizEvaluateResponse { Success = false };

            var system =
                "Bạn là giám khảo giáo dục. Hãy phản hồi CHỈ bằng một JSON hợp lệ đúng schema yêu cầu. " +
                "Tất cả nội dung phải bằng tiếng Việt chuẩn, mạch lạc, có dấu. " +
                "Không chèn code fences, không thêm lời bình. " +
                "Nếu không có URL thì đặt target_url = null.";

            var user = BuildUserPrompt(evt);

            // ✅ Gọi OpenAI Chat với JSON mode
            ChatCompletion completion;
            try
            {
                completion = await chat.CompleteChatAsync(
                    new ChatMessage[]
                    {
                        new SystemChatMessage(system),
                        new UserChatMessage(user)
                    },
                    new ChatCompletionOptions
                    {
                        Temperature = 0.2f,
                        // tương đương response_format = { type = "json_object" }
                        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                    },
                    ct
                );
            }
            catch (Exception ex)
            {
                // tương đương phần catch HttpRequestException cũ
                throw new InvalidOperationException(
                    $"AI request failed: {ex.Message}", ex);
            }

            var content = (completion.Content?.Count > 0)
                ? string.Concat(completion.Content.Select(c => c.Text))
                : null;

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Empty AI content");

            // phòng trường hợp model vẫn chèn ```json
            content = StripCodeFence(content);

            // ✅ parse JSON → DTO (giữ nguyên logic helper cũ)
            var dto = ParseAiJsonSafely(content);

            // Gắn meta giống logic cũ
            dto = dto with
            {
                Model = _opt.Model,
                RubricVersion = _opt.RubricVersion,
                CreatedAtUtc = DateTime.UtcNow
            };

            // Validate tối thiểu
            if (dto.Score100 is < 0 or > 100)
                throw new InvalidOperationException("score100 out of range");

            if (string.IsNullOrWhiteSpace(dto.Summary))
            {
                var score = dto.Score100;
                var sCount = dto.Strengths?.Count ?? 0;
                var iCount = dto.Improvements?.Count ?? 0;

                dto = dto with
                {
                    Summary = $"Điểm tổng {score}/100. {sCount} điểm mạnh, {iCount} điểm cần cải thiện. " +
                              "Hãy ưu tiên xử lý các mục cải thiện trước."
                };
            }

            // ✅ Giữ nguyên event upsert AiEvaluation
            var aiEvaluateUpsertEvent =
                new BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents.AiEvaluationUpsertEvent(
                    AttemptId: evt.AttemptId,
                    UserId: evt.UserId,
                    CourseId: evt.CourseId,
                    Scope: evt.Scope,
                    ScopeId: evt.ScopeId,
                    QuizId: evt.QuizId,
                    Score100: (short)dto.Score100,
                    Score100Raw: evt.Score100Raw,
                    Summary: dto.Summary,
                    Strengths: dto.Strengths ?? [],
                    Improvements: dto.Improvements ?? [],
                    Actions: dto.Actions,
                    SkillGaps: dto.SkillGaps,
                    Confidence: (decimal)dto.Confidence,
                    Model: dto.Model,
                    RubricVersion: dto.RubricVersion,
                    CreatedAtUtc: dto.CreatedAtUtc
                );

            await _publish.Publish(aiEvaluateUpsertEvent, ct);

            response.Success = true;
            response.SetMessage(MessageId.I00000, "Đánh giá quiz thành công");
            response.Response = dto;

            return response;
        }
    }
}
