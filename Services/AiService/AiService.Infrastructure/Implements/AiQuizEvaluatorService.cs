using AiService.Application.DTOs;
using AiService.Application.Handler.Quizzes.Commands;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static AiService.Infrastructure.Helpers.AiQuizEvaluator.AiQuizEvaluatorCommon;

namespace AiService.Infrastructure.Implements
{
	public sealed class AiQuizEvaluatorService(
		GroqOptionsDto _opt,
		HttpClient _http,
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
			var response  = new QuizEvaluateResponse { Success = false };

			var system =
				"Bạn là giám khảo giáo dục. Hãy phản hồi CHỈ bằng một JSON hợp lệ đúng schema yêu cầu. " +
				"Tất cả nội dung phải bằng tiếng Việt chuẩn, mạch lạc, có dấu. " +
				"Không chèn code fences, không thêm lời bình. " +
				"Nếu không có URL thì đặt target_url = null. ";

			var user = BuildUserPrompt(evt);

			var req = new
			{
				model = _opt.Model, // "llama-3.3-70b-versatile"
				messages = new object[]
				{
			new { role = "system", content = system },
			new { role = "user", content = user }
				},
				response_format = new { type = "json_object" }, // JSON mode (không hỗ trợ json_schema)
				temperature = 0.2,
				max_tokens = 600
			};

			using var msg = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
			msg.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

			HttpResponseMessage? resp = null;
			try
			{
				resp = await _http.SendAsync(msg, ct);
				resp.EnsureSuccessStatusCode();
			}
			catch (HttpRequestException ex)
			{
				var status = resp?.StatusCode.ToString() ?? "NoResponse";
				var errBody = resp?.Content is null ? "" : await resp.Content.ReadAsStringAsync(ct);
				throw new InvalidOperationException($"AI request failed: {status} - {errBody}", ex);
			}

			var raw = await resp.Content.ReadAsStringAsync(ct);
			using var root = JsonDocument.Parse(raw);

			// an toàn hơn: kiểm tra choices tồn tại
			if (!root.RootElement.TryGetProperty("choices", out var choices) ||
				choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
				throw new InvalidOperationException("AI response missing choices");

			var content = choices[0].GetProperty("message").GetProperty("content").GetString()
						  ?? throw new InvalidOperationException("Empty AI content");

			content = StripCodeFence(content);  // bỏ ```json ... ```

			// ✅ Parse “an toàn” → trả thẳng DTO đã chuẩn hóa
			var dto = ParseAiJsonSafely(content);

			// Gắn meta
			dto = dto with
			{
				Model = _opt.Model,
				RubricVersion = _opt.RubricVersion,
				CreatedAtUtc = DateTime.UtcNow
			};

			// Validate tối thiểu
			if (dto.Score100 is < 0 or > 100) throw new InvalidOperationException("score100 out of range");

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

			var aiEvaluateUpsertEvent = new BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents.AiEvaluationUpsertEvent(
				AttemptId: evt.AttemptId,
				UserId: evt.UserId,
				CourseId: evt.CourseId,
				Scope: evt.Scope,
				ScopeId: evt.ScopeId,
				QuizId: evt.QuizId,
				Score100: (short)dto.Score100,
				Summary: dto.Summary,
				Strengths: dto.Strengths,
				Improvements: dto.Improvements,
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
