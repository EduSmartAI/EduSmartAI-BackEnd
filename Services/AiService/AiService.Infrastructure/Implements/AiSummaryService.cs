using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse; // NEW
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using MassTransit;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure.Implements
{
    public class AiSummaryService(
        IRequestClient<GetInfoEvaluationEvent> requestClient,
        IRequestClient<GetAllDetailCourseEvent> courseClient,
        ChatClient chat
    ) : IAiSummaryService
    {
        public async Task<AiSummaryResponse> FeedBackCourseByAI(AiSummaryRequest req, CancellationToken ct)
        {
            var @event = new GetInfoEvaluationEvent(req.StudentId, req.CourseId);
            var busResp = await requestClient.GetResponse<GetInfoEvaluationEventResponse>(@event, ct);
            var data = busResp.Message?.Response;

			// 2) Xử lý trường hợp không có dữ liệu
			if (data is null ||
				(data.Lessons.All(g => g.Evaluations.Count == 0) &&
				 data.Modules.All(g => g.Evaluations.Count == 0)))
			{
				return new AiSummaryResponse
				{
					Response = """
                    ## Đánh giá tổng thể khoá học

                    Chưa có dữ liệu đánh giá để tổng hợp. Vui lòng hoàn thành các bài kiểm tra/quiz trước khi yêu cầu AI tóm tắt.
                    """
				};
			}

			// 3) Gom & tính nhanh các chỉ số
			var all = data.Lessons.SelectMany(x => x.Evaluations)
								  .Concat(data.Modules.SelectMany(x => x.Evaluations))
								  .ToList();

			int total = all.Count;
			double avgAdj = Math.Round(all.Average(e => (double)e.Score100), 2);

			// Có thể còn Score100Raw trong dữ liệu, nhưng không hiển thị ra ngoài.
			var raws = all.Where(e => e.Score100Raw.HasValue)
						  .Select(e => (double)e.Score100Raw!.Value)
						  .ToList();

			double? avgDelta = raws.Count > 0
				? Math.Round(all.Where(e => e.Score100Raw.HasValue)
								.Average(e => (double)e.Score100 - e.Score100Raw!.Value), 2)
				: null;
			double? avgScoreRaw = raws.Count > 0 ? Math.Round(raws.Average(), 2) : (double?)null;
			// Phân theo scope
			var lessonAll = data.Lessons.SelectMany(g => g.Evaluations).ToList();
			var moduleAll = data.Modules.SelectMany(g => g.Evaluations).ToList();

			var scopeStats = new
			{
				lesson = new
				{
					count = lessonAll.Count,
					avgScore = lessonAll.Count == 0 ? (double?)null : Math.Round(lessonAll.Average(x => (double)x.Score100), 2)
				},
				module = new
				{
					count = moduleAll.Count,
					avgScore = moduleAll.Count == 0 ? (double?)null : Math.Round(moduleAll.Average(x => (double)x.Score100), 2)
				}
			};

			// Helper: label hiển thị cho group (ưu tiên Name, fallback ScopeId)
			static string GroupLabel(EvaluationGroupDto g) =>
				g.Evaluations.Where(e => !string.IsNullOrWhiteSpace(e.Name))
							 .GroupBy(e => e.Name)
							 .OrderByDescending(gr => gr.Count())
							 .Select(gr => gr.Key)
							 .FirstOrDefault() ?? g.ScopeId.ToString();


			const double RISK_ABS = 60.0;                 // ngưỡng tuyệt đối

			// Top/bottom group theo điểm trung bình (CÓ label)
			var lessonGroups = data.Lessons
			.Where(g => g.Evaluations.Count > 0) // NEW
			.Select(g => new
			{
				scopeId = g.ScopeId,
				label = GroupLabel(g),
				count = g.Evaluations.Count,
				avg = g.Evaluations.Average(x => (double)x.Score100)
			})
			.OrderByDescending(x => x.avg)
			.ToList();

			var moduleGroups = data.Modules
				.Where(g => g.Evaluations.Count > 0) // NEW
				.Select(g => new
				{
					scopeId = g.ScopeId,
					label = GroupLabel(g),
					count = g.Evaluations.Count,
					avg = g.Evaluations.Average(x => (double)x.Score100)
				})
				.OrderByDescending(x => x.avg)
				.ToList();

            var lowLessons = lessonGroups.Where(x => x.avg <= RISK_ABS)
                             .OrderBy(x => x.avg)
                             .Take(3).ToList();
            var lowModules = moduleGroups
                .Where(x => x.avg <= RISK_ABS)
                .OrderBy(x => x.avg)
                .Take(3)
                .ToList();

			// 3.1) Map Lesson -> ModuleName để hiển thị "Module liên quan"
			var lessonModuleMap = new Dictionary<Guid, string>();
			try
			{
				var courseEvt = new GetAllDetailCourseEvent(req.CourseId, req.StudentId);
				var courseResp = await courseClient.GetResponse<GetAllDetailCourseResponse>(courseEvt, ct);
				var course = courseResp.Message?.Response;

				if (course != null)
				{
					var modulesProp = course.GetType().GetProperty("Modules");
					var modules = modulesProp?.GetValue(course) as System.Collections.IEnumerable;
					if (modules != null)
					{
						foreach (var m in modules)
						{
							var mName = (string?)(
								m.GetType().GetProperty("Name")?.GetValue(m) ??
								m.GetType().GetProperty("ModuleName")?.GetValue(m)
							) ?? "—";

							var lessonsProp = m.GetType().GetProperty("Lessons");
							var lessons = lessonsProp?.GetValue(m) as System.Collections.IEnumerable;
							if (lessons == null) continue;

							foreach (var ls in lessons)
							{
								var lessonIdObj =
									ls.GetType().GetProperty("LessonId")?.GetValue(ls) ??
									ls.GetType().GetProperty("Id")?.GetValue(ls);

								if (lessonIdObj is Guid lessonId)
									lessonModuleMap[lessonId] = mName;
							}
						}
					}
				}
			}
			catch
			{
				// fallback giữ "—" nếu không lấy được cây khoá học
			}

			// 4) Rút trích gạch đầu dòng (strengths, improvements, actions, skill gaps)
			static IEnumerable<string> SplitBullets(string s)
			{
				if (string.IsNullOrWhiteSpace(s)) yield break;
				var parts = s.Replace("\r", "")
							 .Split(new[] { '\n', ';', '•', '-' }, StringSplitOptions.RemoveEmptyEntries)
							 .Select(x => x.Trim().TrimEnd('.'))
							 .Where(x => x.Length > 0);
				foreach (var p in parts) yield return p;
			}

			IEnumerable<string> collect(IEnumerable<string> src) =>
				src.SelectMany(SplitBullets)
				   .Select(x => x.Length > 120 ? x[..120] + "…" : x);

			var strengths = collect(all.Select(x => x.Strengths)).Take(50).ToList();
			var improvements = collect(all.Select(x => x.Improvements)).Take(50).ToList();
			var actions = collect(all.Select(x => x.Actions)).Take(50).ToList();
			var gaps = collect(all.Select(x => x.SkillGaps)).Take(50).ToList();
			var lowLessonScopeIds = new HashSet<Guid>(
				lowLessons.Where(x => x.scopeId.HasValue).Select(x => x.scopeId!.Value)
			);
			var lowModuleScopeIds = new HashSet<Guid>(
				lowModules.Where(x => x.scopeId.HasValue).Select(x => x.scopeId!.Value)
			);

			var lowLessonEvals = lessonAll
				.Where(e => e.ScopeId.HasValue && lowLessonScopeIds.Contains(e.ScopeId.Value))
				.ToList();

			var lowModuleEvals = moduleAll
				.Where(e => e.ScopeId.HasValue && lowModuleScopeIds.Contains(e.ScopeId.Value))
				.ToList();


			double? minLessonAvg = lowLessons.Count > 0 ? Math.Round(lowLessons.Min(x => x.avg), 2) : (double?)null;
			double? maxLessonAvg = lowLessons.Count > 0 ? Math.Round(lowLessons.Max(x => x.avg), 2) : (double?)null;
			double? minModuleAvg = lowModules.Count > 0 ? Math.Round(lowModules.Min(x => x.avg), 2) : (double?)null;
			double? maxModuleAvg = lowModules.Count > 0 ? Math.Round(lowModules.Max(x => x.avg), 2) : (double?)null;

			string GetModuleName(Guid? lessonScopeId) =>
						lessonScopeId.HasValue && lessonModuleMap.TryGetValue(lessonScopeId.Value, out var name)
						? name
						: "—";


			var topModuleNames = lowLessons
					.Select(x => GetModuleName(x.scopeId))
					.GroupBy(n => n)
					.Select(g => new { name = g.Key, count = g.Count() })
					.OrderByDescending(g => g.count)
					.Take(3)
					.ToList();


			// Tận dụng hàm collect(...) có sẵn để rút gọn gạch đầu dòng
			var lowLessonIssues = collect(lowLessonEvals.Select(x => x.SkillGaps)
										 .Concat(lowLessonEvals.Select(x => x.Improvements)))
								  .Take(6).ToList();

			var lowModuleIssues = collect(lowModuleEvals.Select(x => x.SkillGaps)
										 .Concat(lowModuleEvals.Select(x => x.Improvements)))
								  .Take(6).ToList();
			// 5) Chuẩn hoá payload JSON cho prompt (không lộ raw)
			var payload = new
			{
				courseId = req.CourseId,
				totalEvaluations = total,
				avgScoreAI = avgAdj,
				avgScoreRaw,
				avgAdjustDelta = avgDelta,
				scopeStats,
				// ⬇️ CHỈ GIỮ riskGroups
				riskGroups = new
				{
					lessons = lowLessons.Select(x => new
					{
						x.label,
						x.count,
						avgAI = Math.Round(x.avg, 2),
						module = GetModuleName(x.scopeId)
					}),
					modules = lowModules.Select(x => new
					{
						x.label,
						x.count,
						avgAI = Math.Round(x.avg, 2)
					})
				},
				riskAnalysis = new
				{
					lessons = new
					{
						count = lowLessons.Count,
						minAvg = minLessonAvg,
						maxAvg = maxLessonAvg,
						names = lowLessons.Select(x => x.label),
						topModules = topModuleNames,          // [{ name, count }]
						commonIssues = lowLessonIssues        // tối đa 6 bullet ngắn
					},
					modules = new
					{
						count = lowModules.Count,
						minAvg = minModuleAvg,
						maxAvg = maxModuleAvg,
						names = lowModules.Select(x => x.label),
						commonIssues = lowModuleIssues        // tối đa 6 bullet ngắn
					}
				},
				samples = all
				.OrderByDescending(x => x.CreatedAt)
				.Take(16)
				.Select(x => new
				{
					x.EvaluationId,
					x.AttemptId,
					x.QuizId,
					name = x.Name,
					scoreAI = x.Score100,
					confidence = x.Confidence,
					scope = x.Scope,
					scopeId = x.ScopeId,
					createdAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
					summary = Ellipsis(x.Summary, 160),
					strengths = Ellipsis(x.Strengths, 120),
					improvements = Ellipsis(x.Improvements, 120),
					actions = Ellipsis(x.Actions, 120),
					gaps = Ellipsis(x.SkillGaps, 120),
					model = x.Model,
					rubricVersion = x.RubricVersion
				}),
				bullets = new { strengths, improvements, actions, skillGaps = gaps },
				hasLessonRisks = lowLessons.Count > 0,
				hasModuleRisks = lowModules.Count > 0,
				legend = "Điểm hiển thị là 'điểm do AI chấm' (score100). Không hiển thị điểm gốc."
			};

			static string Ellipsis(string s, int max) =>
				string.IsNullOrWhiteSpace(s) ? "" : (s.Length <= max ? s : s[..max] + "…");


			var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = false,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			});
			Console.WriteLine(json);

			// 6) Prompts — cấm lộ raw, dùng “điểm do AI chấm”
			var systemPrompt = """
            Bạn là trợ giảng AI viết báo cáo tóm tắt kết quả học tập bằng tiếng Việt.

            QUY TẮC BẮT BUỘC:
            - Chỉ trả về Markdown, không kèm giải thích ngoài văn bản.
            - Không nhắc đến tên trường dữ liệu kỹ thuật (ScopeId, QuizId...).
            - Mặc định gọi score100 là **"điểm do AI chấm"**.
            - **ĐƯỢC PHÉP** hiển thị **"Điểm thô trung bình"** nếu JSON có `avgScoreRaw`; tuyệt đối **không** hiển thị điểm thô của từng bài.
            - Nếu có `avgAdjustDelta`: chỉ diễn đạt là **"mức hiệu chỉnh trung bình"**, không nói nguồn/tham số.
            - Ưu tiên rõ ràng, súc tích; bullet bắt đầu bằng động từ; có tiêu đề cấp 2/3 rõ ràng.
            - Nếu một mục không có dữ liệu, hãy ghi chú ngắn gọn thay vì để trống.
            - Ở mục **Nhóm rủi ro cao**, CHỈ dùng `riskGroups.lessons` và `riskGroups.modules`.
            - TUYỆT ĐỐI KHÔNG thêm dòng từ trường khác (vd: `topGroups`) hay tự suy đoán.
            - Số dòng của mỗi bảng = đúng độ dài mảng tương ứng; không tự thêm cho đủ 3.
            """;

			var userPrompt = $$"""
            DỮ LIỆU (JSON, đã chuẩn hoá):Điểm hiện tại là 'điểm do AI chấm'. Khống hiển thị điểm gốc.
            
            ```json
            {{json}}
            ```

            Hãy viết **báo cáo đánh giá tổng thể khoá học** theo bố cục sau, dùng tiếng Việt, ngắn gọn, rõ ràng:

            ## Tổng quan
            - Số bài đã chấm và **điểm do AI chấm** (trung bình) (3–5 câu).
            - Nếu có: nêu 1 câu về **mức hiệu chỉnh trung bình** (không giải thích kỹ thuật).

            ### Bảng tổng quan
            > Điền giá trị cụ thể từ JSON, không để placeholder.
            | Chỉ số | Giá trị |
            |---|---|
            | Số đánh giá | dùng `totalEvaluations` |
            | Điểm AI trung bình | dùng `avgScoreAI` |
            | Điểm thô trung bình (Score100Raw) | nếu JSON có (vd: avgScoreRaw) → số (0–100, làm tròn 2); nếu không → "—" |
            | Mức hiệu chỉnh trung bình | nếu có avgAdjustDelta → số làm tròn 2; nếu không → "—" |
            | Số bài theo scope | Lesson: `scopeStats.lesson.count` · Module: `scopeStats.module.count` |
            | Ghi chú | `legend` |

            ### Nhận xét tổng quan
            - Viết **2–3 câu** nhận định chung về kết quả học tập và xu hướng điểm (không lặp lại y nguyên số trong bảng).

            ## Điểm mạnh nổi bật
            - 3–6 bullet súc tích, tổng hợp từ strengths/summary (ưu tiên kỹ năng/kiến thức hành xử tốt).

            ## Vấn đề & Khoảng trống kỹ năng
            - 3–6 bullet rõ ràng, chỉ ra lỗi thường gặp và hệ quả.

            ## Phân tầng chất lượng
            - Hãy phân loại mang tính định tính theo ngưỡng tham khảo:

            - Xuất sắc: ≥ 85 · Vững: 70–84 · Cần củng cố: 50–69 · Nguy cơ: < 50.

            - Viết 1 đoạn ngắn (4-8 câu) nêu tỷ trọng ước lượng dựa trên các mẫu gần nhất (samples) và hàm ý học tập, Đoạn văn này là phân tích chất lượng làm bài của người dùng hiện tại. Nếu số mẫu < 6, nêu hạn chế dữ liệu.

            ## Ưu tiên hành động (1–2 tuần)
            - 4–6 bullet, bắt đầu bằng động từ (Ôn, Luyện, Làm, Viết, Thực hành).
            - Ghép hành động với module/lesson còn yếu (ưu tiên từ riskGroups), nêu đầu mục kiến thức và khối lượng luyện tập (vd: “mỗi ngày 2–3 bài ngắn”).

            ## Nhóm rủi ro cao
            Trình bày tách **hai bảng riêng**:

            ### 🔹 Lesson có điểm thấp
            - Nếu `hasLessonRisks` = false → _"Không có lesson nào ở mức rủi ro."_
            - Ngược lại, vẽ bảng **chỉ từ** `riskGroups.lessons`.
            - **Số dòng = riskGroups.lessons.length; không tự thêm để đủ 3. Không suy đoán.**

            | Lesson | Module liên quan | Điểm AI TB | Số bài | Đánh giá ngắn |
            |---|---|---|---|---|
            | (riskGroups.lessons[i].label) | (riskGroups.lessons[i].module) | (riskGroups.lessons[i].avgAI) | (riskGroups.lessons[i].count) | 1 câu nhận xét ngắn |

            **Phân tích nhanh (Lesson)**
            - Dùng **chỉ** `riskAnalysis.lessons`. Nếu `count` = 0 → in "—".
            - 3–5 bullet:
              - Số lượng lesson rủi ro và khoảng điểm TB: `(minAvg – maxAvg)`.
              - Chủ đề/module lặp lại đáng chú ý từ `topModules` (ghi dạng: “{name}: {count} lesson”).
              - 1–2 vấn đề phổ biến rút gọn từ `commonIssues`.
              - Gợi ý trọng tâm 1 câu (không chèn link).

            ### 🔸 Module có điểm thấp
            - Nếu `hasModuleRisks` là **false** → in: _"Không có module nào ở mức rủi ro."_ và **không vẽ bảng**.
            - Ngược lại, vẽ bảng **chỉ** từ `riskGroups.modules`:

            | Module | Điểm AI TB | Số bài | Đánh giá ngắn |
            |---|---|---|---|
            | (riskGroups.modules[i].label) | (riskGroups.modules[i].avgAI) | (riskGroups.modules[i].count) | 1 câu nhận xét ngắn |

            **Phân tích nhanh (Module)**
            - Dùng **chỉ** `riskAnalysis.modules`. Nếu `count` = 0 → in "—".
            - 2–4 bullet nêu: số lượng, khoảng điểm `(minAvg – maxAvg)`, vấn đề phổ biến từ `commonIssues`, 1 câu gợi ý ưu tiên.

            ## Nguyên nhân gốc
            - 2–4 bullet có căn cứ, tổng hợp từ Vấn đề & Khoảng trống và Nhóm rủi ro (vd: thiếu nền tảng khái niệm, đọc hiểu đề yếu, thời gian luyện không đều…).
            
            ## Xu hướng theo thời gian
            - Dựa vào samples.createdAt (tối đa 16 mẫu mới nhất): nêu xu hướng gần đây (↑/→/↓) so với trung bình; nếu số mẫu ít (n < 6) ghi "—" hoặc nhận định dè dặt.

            ## Gợi ý học tập nhanh
            - 2–3 bullet: nguồn/kiểu bài tập/nhịp luyện tập đề xuất.

            ## Ghi chú
            - Điểm hiển thị là **"điểm do AI chấm"**. Không hiển thị điểm gốc hay thuật ngữ kỹ thuật.
            """;

			// 7) Gọi OpenAI Chat
			string markdown;
			try
			{
				ChatCompletion result = await chat.CompleteChatAsync(
					new ChatMessage[]
					{
						new SystemChatMessage(systemPrompt),
						new UserChatMessage(userPrompt)
					},
					new ChatCompletionOptions { Temperature = 0.2f },
					ct
				);

				var text = (result?.Content?.Count > 0)
					? string.Concat(result.Content.Select(c => c.Text))
					: null;

				markdown = string.IsNullOrWhiteSpace(text)
					? FallbackMarkdown(avgAdj, scopeStats.lesson.count, scopeStats.module.count)
					: text!;
			}
			catch
			{
				markdown = FallbackMarkdown(avgAdj, scopeStats.lesson.count, scopeStats.module.count);
			}

			return new AiSummaryResponse { Response = markdown };
		}

		private static string FallbackMarkdown(double avgAdj, int lessonsCount, int modulesCount) => $"""
        ## Đánh giá tổng thể khoá học (bản rút gọn)

        - Điểm do AI chấm (trung bình): **{avgAdj}**
        - Phạm vi: {lessonsCount} lesson, {modulesCount} module

        **Gợi ý nhanh**
        - Tập trung ôn các nhóm có điểm trung bình thấp trước.
        - Lên kế hoạch luyện 2–3 bài/ngày trong 1–2 tuần, theo đúng chủ đề còn yếu.
        """;

        public async Task<string> GenerateProgressFeedbackMarkdownAsync(
            AiSummaryFeedbackModuleDto req,
            CancellationToken ct = default)
        {
            // đảm bảo không null
            var strengths = req.Strengths ?? Enumerable.Empty<string>();
            var improvements = req.Improvements ?? Enumerable.Empty<string>();
            var actions = req.Actions ?? Enumerable.Empty<string>();
            var skillGaps = req.SkillGaps ?? Enumerable.Empty<string>();

            // Lấy detail course để biết module + lesson
            var courseEvt = new GetAllDetailCourseEvent(req.CourseId, req.StudentId);
            var courseResp = await courseClient.GetResponse<GetAllDetailCourseResponse>(courseEvt, ct);
            var course = courseResp.Message?.Response;

            // Module hiện tại
            var targetModule = course?.Modules
                .FirstOrDefault(m => m.ModuleId == req.ModuleId && m.Lessons.Any());

            // Chuẩn hoá moduleInfo (để AI đề xuất học lại theo link)
            var moduleInfo = targetModule == null
                ? null
                : new
                {
                    moduleId = targetModule.ModuleId,
                    moduleName = targetModule.ModuleName,
                    lessons = targetModule.Lessons.Select(l => new
                    {
                        lessonId = l.LessonId,
                        lessonName = l.LessonTitle,
                        url = $"https://www.edusmart.pro.vn/course/{req.CourseId}/learn?lessonId={l.LessonId}",
                    })
                    .ToList()
                };

            string masteryLevel;

            if (req.Score100 >= 95)
            {
                masteryLevel = "excellent";
            }
            else if (req.Score100 >= 85)
            {
                masteryLevel = "strong";
            }
            else if (req.Score100 >= 70)
            {
                masteryLevel = "solid";
            }
            else if (req.Score100 >= 50)
            {
                masteryLevel = "weak";
            }
            else
            {
                masteryLevel = "struggling";
            }
            if (masteryLevel is "excellent" or "strong")
            {
                improvements = improvements
                    .Where(x => !x.Contains("đọc lại", StringComparison.OrdinalIgnoreCase)
                             && !x.Contains("xem lại", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                actions = actions
                    .Where(x => !x.Contains("đọc lại", StringComparison.OrdinalIgnoreCase)
                             && !x.Contains("xem lại", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            var payload = new
            {
                lessonsTotal = req.LessonsTotal,
                lessonsCompleted = req.LessonsCompleted,
                percentCompleted = req.PercentCompleted,
                score100Raw = req.Score100Raw,   // điểm gốc
                score100 = req.Score100,      // điểm do AI chấm (đã hiệu chỉnh)
                strengths = strengths.ToList(),
                improvements = improvements.ToList(),
                actions = actions.ToList(),
                skillGaps = skillGaps.ToList(),
                moduleInfo = moduleInfo,            // ⬅️ thêm vào JSON
                masteryLevel = masteryLevel,
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var systemPrompt = """
            Bạn là trợ giảng AI, nhiệm vụ là viết feedback tiến độ học cho người học bằng tiếng Việt, dạng Markdown.

            QUY TẮC:
            - Chỉ trả về **Markdown**, không giải thích thêm.
            - Gọi `score100Raw` là **"điểm gốc"**.
            - Gọi `score100` là **"điểm do AI chấm (đã hiệu chỉnh theo độ khó)"**.
            - Không nhắc lại tên field kỹ thuật (score100Raw, score100...) trong nội dung; chỉ dùng cách gọi tự nhiên ở trên.
            - Viết ngắn gọn, rõ ràng, ưu tiên gạch đầu dòng hành động cụ thể.
            - Dựa vào `masteryLevel` trong JSON:
            - Nếu `masteryLevel` = "excellent" hoặc "strong":
            - Không dùng các cụm như "xem lại bài cơ bản", "củng cố kiến thức nền" trừ khi JSON có skillGaps rõ ràng.
            - Phần "Cần cải thiện" chỉ nói về tinh chỉnh hoặc thử thách nâng cao (chiến lược làm bài, tốc độ, áp dụng thực tế...).
            - Phần "Hành động đề xuất" và "Mục tiêu 7–14 ngày tới" phải ưu tiên mở rộng: làm bài khó hơn, áp dụng vào mini-project, luyện thêm dạng nâng cao.
            - Nếu `masteryLevel` = "weak" hoặc "struggling":
            - Ưu tiên gợi ý ôn lại, củng cố nền tảng, luyện các bài cơ bản.
            """;

            var userPrompt = $$"""
        DỮ LIỆU TIẾN ĐỘ (JSON):

        ```json
        {{json}}
        ```

        Hãy viết feedback tổng hợp cho **1 học viên** theo bố cục:

        ## Tổng quan tiến độ
        - 2–3 câu nhận xét về số bài đã hoàn thành, phần trăm hoàn thành và mức độ nỗ lực.
        - 1 câu so sánh ngắn giữa *điểm gốc* và *điểm do AI chấm (đã hiệu chỉnh theo độ khó)*, nhấn mạnh ý nghĩa (ví dụ: giữ vững, cải thiện, cần cố gắng hơn).

        ## Điểm mạnh
        - 3–5 gạch đầu dòng, tổng hợp từ danh sách strengths (nếu có). Nếu không có dữ liệu, hãy ghi 1–2 ý tích cực chung dựa trên tiến độ.

        ## Cần cải thiện
        - 3–5 gạch đầu dòng từ improvements/skillGaps (nếu có), tập trung vào lỗi, thói quen hoặc kỹ năng còn yếu.

        ## Hành động đề xuất (1–2 tuần tới)
        - 4–6 gạch đầu dòng, bắt đầu bằng động từ (Ôn, Luyện, Làm, Viết, Thực hành…).
        - Mỗi bullet nên có: nội dung cần làm + khối lượng gợi ý (ví dụ: "mỗi ngày 2–3 bài", "luyện 15–20 phút").
        - Nếu JSON có `moduleInfo.lessons`, hãy ưu tiên gợi ý cụ thể các bài trong module này.

        ## Mục tiêu 7–14 ngày tới
        - Viết 2–3 bullet, mỗi bullet là 1 mục tiêu cụ thể, đo được
          (ví dụ: “Hoàn thành thêm 3 bài trong chương X”, 
          “Mỗi ngày dành 20 phút luyện lại dạng bài Y”).
        - Mỗi mục tiêu nên kèm mốc thời gian rõ ràng (trong 7 ngày / 2 tuần).

        ## Câu hỏi tự phản chiếu
        - Đưa ra 2–3 câu hỏi ngắn để người học tự suy nghĩ
          (ví dụ: “Phần nào bạn thấy tốn nhiều thời gian nhất?”, 
          “Thói quen học nào khiến bạn dễ xao nhãng?”,
          “Nếu chỉ chọn 1 kỹ năng để cải thiện tuần này, bạn sẽ chọn gì?”).
        - Viết theo kiểu thân thiện, không phán xét.

        ## Bài học nên ưu tiên trong module hiện tại
        - Nếu `moduleInfo` khác null:
          - Duyệt `moduleInfo.lessons`.
          - Nếu `masteryLevel` = "excellent" hoặc "strong":
            - Chọn tối đa 3–5 bài từ `moduleInfo.lessons`.
            - Mỗi bullet **bắt buộc** dùng cú pháp link Markdown:
              `[Tên bài]({url}) – 1 câu lý do theo hướng đào sâu / luyện nâng cao`.
          - Nếu `masteryLevel` = "weak" hoặc "struggling":
            - Chọn tối đa 3–5 bài từ `moduleInfo.lessons`.
            - Mỗi bullet **bắt buộc** dùng cú pháp link Markdown:
              `[Tên bài]({url}) – 1 câu lý do theo hướng củng cố nền tảng`.
        - Nếu không có `moduleInfo` hoặc không có `moduleInfo.lessons` thì bỏ qua mục này.

        Kết thúc bằng 1 câu động viên ngắn gọn, tích cực.
        """;

            try
            {
                ChatCompletion result = await chat.CompleteChatAsync(
                    new ChatMessage[]
                    {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
                    },
                    new ChatCompletionOptions
                    {
                        Temperature = 0.25f
                    },
                    ct
                );

                var text = (result?.Content?.Count > 0)
                    ? string.Concat(result.Content.Select(c => c.Text))
                    : null;

                return string.IsNullOrWhiteSpace(text)
                    ? BasicProgressFallbackMarkdown(
                        req.LessonsTotal,
                        req.LessonsCompleted,
                        req.PercentCompleted,
                        req.Score100Raw,
                        req.Score100)
                    : text!;
            }
            catch
            {
                return BasicProgressFallbackMarkdown(
                    req.LessonsTotal,
                    req.LessonsCompleted,
                    req.PercentCompleted,
                    req.Score100Raw,
                    req.Score100);
            }
        }

        private static string BasicProgressFallbackMarkdown(
            int lessonsTotal,
            int lessonsCompleted,
            double percentCompleted,
            double score100Raw,
            double score100) => $"""
            ## Tổng quan tiến độ (bản rút gọn)

            - Bài đã hoàn thành: **{lessonsCompleted}/{lessonsTotal}** (~{percentCompleted:F1}%).
            - Điểm gốc: **{score100Raw:F1}**.
            - Điểm do AI chấm (đã hiệu chỉnh theo độ khó): **{score100:F1}**.

            **Gợi ý nhanh**
            - Duy trì nhịp học đều mỗi ngày.
            - Ưu tiên ôn lại các phần còn cảm thấy khó, kết hợp làm thêm bài luyện tập ngắn.
            """;
    }
}
