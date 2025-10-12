using AiService.Application.DTOs;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using System.Text.Json;

namespace AiService.Infrastructure.Helpers.AiQuizEvaluator
{
	public static class AiQuizEvaluatorCommon
	{
		#region Helper Methods
		public static string BuildUserPrompt(QuizEvaluableCreatedEvent evt)
		{
			var raw = new
			{
				language = "vi",                // <- quan trọng
				rubric_version = "quiz-v2.1",
				normalization = "Chuyển điểm thô thành score100 (0..100). strengths<=3, improvements<=4, actions 3-5.",
				context = "Đánh giá kết quả làm bài và đưa khuyến nghị ngắn gọn, súc tích.",
				input = new
				{
					course_id = evt.CourseId,
					scope = evt.Scope.ToString(),
					scope_id = evt.ScopeId,
					attempt_id = evt.AttemptId,
					quiz_id = evt.QuizId,
					score = new { correct = evt.TotalCorrectAnswers, total = evt.TotalQuestions },
					questions = evt.Questions.Select(q => new {
						id = q.QuestionId,
						type = q.QuestionType,
						text = q.QuestionText,
						explanation = q.Explanation,
						answers = q.Answers.Select(a => new {
							id = a.AnswerId,
							text = a.AnswerText,
							is_correct = a.IsCorrectAnswer,
							selected = a.SelectedByStudent
						})
					})
				},
				// Nhắc rõ khóa JSON cần trả (tiếng Việt nhưng key vẫn tiếng Anh)
				required_keys = "summary, strengths, improvements, actions, skill_gaps, score100, confidence"
			};

			return JsonSerializer.Serialize(raw);
		}



		public static AiEvaluationDto ParseAiJsonSafely(string json)
		{
			using var doc = JsonDocument.Parse(json);
			var r = doc.RootElement;

			int score100 = ReadInt(r, "score100", 0);
			double confidence = ReadDouble(r, "confidence", 0.0);
			string summary = ReadString(r, "summary") ?? "";

			var strengths = ReadStringList(r, "strengths");
			var improvements = ReadStringList(r, "improvements");
			var actions = ReadActions(r, "actions");
			var gaps = ReadGaps(r, "skill_gaps");

			return new AiEvaluationDto(
				Score100: score100,
				Summary: summary,
				Strengths: strengths,
				Improvements: improvements,
				Actions: actions,
				SkillGaps: gaps,
				Confidence: confidence,
				Model: "",            // sẽ fill ở trên
				RubricVersion: "",    // sẽ fill ở trên
				CreatedAtUtc: default // sẽ fill ở trên
			);
		}
		public static string StripCodeFence(string s)
		{
			if (s.StartsWith("```", StringComparison.Ordinal))
			{
				var i = s.IndexOf('\n');
				if (i >= 0) s = s[(i + 1)..];
				var last = s.LastIndexOf("```", StringComparison.Ordinal);
				if (last >= 0) s = s[..last];
			}
			return s.Trim();
		}

		public static int ReadInt(JsonElement obj, string name, int def)
		{
			if (!obj.TryGetProperty(name, out var el)) return def;
			return el.ValueKind switch
			{
				JsonValueKind.Number when el.TryGetInt32(out var i) => i,
				JsonValueKind.String when int.TryParse(el.GetString(), out var i) => i,
				_ => def
			};
		}
		public static double ReadDouble(JsonElement obj, string name, double def)
		{
			if (!obj.TryGetProperty(name, out var el)) return def;
			return el.ValueKind switch
			{
				JsonValueKind.Number => el.GetDouble(),
				JsonValueKind.String when double.TryParse(el.GetString(), out var d) => d,
				_ => def
			};
		}
		public static string? ReadString(JsonElement obj, string name)
		{
			if (!obj.TryGetProperty(name, out var el)) return null;
			return el.ValueKind switch
			{
				JsonValueKind.String => el.GetString(),
				JsonValueKind.Number => el.ToString(),
				JsonValueKind.True or JsonValueKind.False => el.GetBoolean().ToString(),
				_ => null
			};
		}

		public static List<string> ReadStringList(JsonElement obj, string name)
		{
			var list = new List<string>();
			if (!obj.TryGetProperty(name, out var el)) return list;

			if (el.ValueKind == JsonValueKind.Array)
			{
				foreach (var item in el.EnumerateArray())
				{
					list.Add(item.ValueKind switch
					{
						JsonValueKind.String => item.GetString()!,
						JsonValueKind.Number => item.ToString(),
						JsonValueKind.True or JsonValueKind.False => item.GetBoolean().ToString(),
						JsonValueKind.Object => item.ToString(),
						_ => ""
					});
				}
			}
			else
			{
				var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
				if (!string.IsNullOrWhiteSpace(s)) list.Add(s!);
			}
			return list.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		}

		public static List<RecommendedAction> ReadActions(JsonElement obj, string name)
		{
			var list = new List<RecommendedAction>();
			if (!obj.TryGetProperty(name, out var el)) return list;

			if (el.ValueKind != JsonValueKind.Array)
			{
				var title = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
				if (!string.IsNullOrWhiteSpace(title))
					list.Add(new RecommendedAction(title!, "tip", null));
				return list;
			}

			foreach (var item in el.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.Object)
				{
					var title = ReadString(item, "title") ?? ReadString(item, "text") ?? "Suggestion";
					var kind = ReadString(item, "kind") ?? "tip";
					var url = ReadString(item, "target_url") ?? ReadString(item, "url") ?? ReadString(item, "link");
					list.Add(new RecommendedAction(title, kind, string.IsNullOrWhiteSpace(url) ? null : url));
				}
				else if (item.ValueKind == JsonValueKind.String)
				{
					list.Add(new RecommendedAction(item.GetString()!, "tip", null));
				}
				else
				{
					list.Add(new RecommendedAction(item.ToString(), "tip", null));
				}
			}
			return list;
		}

		public static List<SkillGap> ReadGaps(JsonElement obj, string name)
		{
			var list = new List<SkillGap>();
			if (!obj.TryGetProperty(name, out var el)) return list;
			if (el.ValueKind != JsonValueKind.Array) return list;

			foreach (var item in el.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.Object)
				{
					var tag = ReadString(item, "skill_tag") ?? ReadString(item, "skill") ?? "unknown";
					var level = ReadInt(item, "level", 1);
					var evidence = ReadString(item, "evidence") ?? "";
					list.Add(new SkillGap(tag, level, evidence));
				}
				else
				{
					var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
					list.Add(new SkillGap(s ?? "unknown", 1, ""));
				}
			}
			return list;
		}

		#endregion
	}
}
