using AiService.Application.Features.AiRecommend;
using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using AiService.Infrastructure.Helpers.AiQuizEvaluator;
using AiService.Infrastructure.Prompts;
using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.AIService.SubjectInfoEvent;
using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse; // NEW
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using BuildingBlocks.Messaging.Events.StudentService.GetOverviewAiEvaluation;
using MassTransit;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AiService.Infrastructure.Implements
{
    public class AiSummaryService(
        IRequestClient<GetInfoEvaluationEvent> requestClient,
        IRequestClient<GetAllDetailCourseEvent> courseClient,
        IRequestClient<SubjectInfoEvent> subjectInfoClient,
        IRequestClient<GetOverviewAiEvaluationEvent> overviewAiEvaluationClient,
        IRequestClient<GetInfoInternalCourseEvents> getInfoInternalCourseClient,
        IIdentityService identityService,
        ChatClient chat
    ) : IAiSummaryService
    {
        private const int SubjectsPerPrompt = 6;
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


            const double RISK_ABS = 6.0;                 // ngưỡng tuyệt đối

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

            - Xuất sắc: ≥ 8.5 · Vững: 7.0–8.4 · Cần củng cố: 5.0–6.9 · Nguy cơ: < 5.0.

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

            if (req.Score100 >= 9.5)
            {
                masteryLevel = "excellent";
            }
            else if (req.Score100 >= 8.5)
            {
                masteryLevel = "strong";
            }
            else if (req.Score100 >= 7.0)
            {
                masteryLevel = "solid";
            }
            else if (req.Score100 >= 5.0)
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

        #region Sinh feedback học tập
        /// <summary>
        /// Generate feedback overall
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<AiRecommendImprovementResponse> GenerateLearningFeedbackMarkdownAsync(
            AiRecommendImprovementRequest req,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(req);

            var curriculumSubjects = await LoadCurriculumSubjectsAsync(req, ct);
            var abilityMarks = EnsureAbilityCoverage(req.AbilityMarks);
            var subjectMarks = EnsureSubjectCoverage(req.SubjectMarks, curriculumSubjects);
            var scoredSubjectMarks = subjectMarks.Where(m => m.Mark.HasValue).ToList();
            var missingSubjectMarks = subjectMarks.Where(m => !m.Mark.HasValue).ToList();
            var quizSurvey = req.QuizSurvey;
            var careerGoal = req.CareerGoal?.Trim() ?? string.Empty;

            var subjectBatches = ChunkSubjects(scoredSubjectMarks, SubjectsPerPrompt).ToList();

            var abilityTask = CompleteJsonChatAsync(
                AiRecommendPromptLibrary.SystemPrompt,
                AiRecommendPromptLibrary.BuildAbilityPrompt(abilityMarks, careerGoal),
                ct);

            var personaTask = CompleteJsonChatAsync(
                AiRecommendPromptLibrary.SystemPrompt,
                AiRecommendPromptLibrary.BuildPersonaPrompt(subjectMarks, abilityMarks, quizSurvey, careerGoal),
                ct);

            var subjectPromptTasks = subjectBatches
                .Select(batch => CompleteJsonChatAsync(
                    AiRecommendPromptLibrary.SystemPrompt,
                    AiRecommendPromptLibrary.BuildSubjectPrompt(batch, curriculumSubjects, careerGoal),
                    ct))
                .ToList();

            Task<string?>? dependencyTask = null;
            if (missingSubjectMarks.Count > 0)
            {
                dependencyTask = CompleteJsonChatAsync(
                    AiRecommendPromptLibrary.SystemPrompt,
                    AiRecommendPromptLibrary.BuildMissingSubjectPrompt(missingSubjectMarks, curriculumSubjects, careerGoal),
                    ct);
            }

            var waitTasks = new List<Task>(subjectPromptTasks.Count + 2 + (dependencyTask is null ? 0 : 1));
            waitTasks.AddRange(subjectPromptTasks);
            waitTasks.Add(abilityTask);
            waitTasks.Add(personaTask);
            if (dependencyTask is not null)
            {
                waitTasks.Add(dependencyTask);
            }
            await Task.WhenAll(waitTasks);

            var abilityPayload = await abilityTask;
            var personaPayload = await personaTask;
            var dependencyPayload = dependencyTask is null ? null : await dependencyTask;

            var subjectAnalyses = new List<SubjectAnalysis>();
            foreach (var task in subjectPromptTasks)
            {
                var payload = task.Result;
                var parsed = TryParseSubjectAnalyses(payload);
                if (parsed is { Count: > 0 })
                {
                    subjectAnalyses.AddRange(parsed);
                }
            }

            if (subjectAnalyses.Count == 0)
            {
                subjectAnalyses = BuildSubjectFallback(scoredSubjectMarks, curriculumSubjects, careerGoal);
            }
            else
            {
                EnrichSubjectAnalysesWithAcademicData(subjectAnalyses, curriculumSubjects, scoredSubjectMarks);
            }

            var abilityAnalyses = TryParseAbilityAnalyses(abilityPayload) ?? BuildAbilityFallback(abilityMarks, careerGoal);
            var personaSummary = TryParsePersonaSummary(personaPayload) ?? BuildPersonaFallback(subjectMarks, abilityMarks, quizSurvey, careerGoal);
            var withoutMarkAnalyses = missingSubjectMarks.Count == 0
                ? new List<SubjectWithoutMarkAnalysis>()
                : TryParseWithoutMarkAnalyses(dependencyPayload) ?? BuildWithoutMarkFallback(missingSubjectMarks, curriculumSubjects, careerGoal);

            return new AiRecommendImprovementResponse
            {
                Success = true,
                Response = new AiAnalysisSubjectAndAbilityDto
                {
                    SummaryFeedback = personaSummary.summaryFeedback,
                    HabitAndInterestAnalysis = personaSummary.habitAndInterestAnalysis,
                    Personality = personaSummary.personality,
                    LearningAbility = personaSummary.learningAbility,
                    SubjectAnalyses = subjectAnalyses,
                    AbilityAnalyses = abilityAnalyses,
                    WithoutMarkAnalysis = withoutMarkAnalyses
                }
            };
        }
        #endregion

        #region Subject Analysis
        public async Task<SubjectAnalysisResponse> AnalyzeSubjectMarkAsync(
            SubjectAnalysisRequest req,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(req);

            var normalizedSubjectCode = NormalizeSubjectCode(req.SubjectCode);
            if (string.IsNullOrWhiteSpace(normalizedSubjectCode))
            {
                return new SubjectAnalysisResponse
                {
                    Success = false,
                    Response = new SubjectAnalysisDto
                    {
                        SubjectCode = req.SubjectCode,
                        SubjectName = req.SubjectName,
                        Mark = req.Mark,
                        ImprovementAnalysis = "Mã môn học không hợp lệ."
                    }
                };
            }

            // 1. Load curriculum subjects để lấy thông tin về môn phụ thuộc
            var curriculumSubjects = await LoadCurriculumSubjectsForAnalysisAsync(
                normalizedSubjectCode,
                req.MajorCode,
                ct);

            // 2. Tìm các môn phụ thuộc (subjects that depend on this subject)
            var dependentsLookup = BuildDependentsLookupForAnalysis(curriculumSubjects);
            dependentsLookup.TryGetValue(normalizedSubjectCode, out var dependents);

            // 3. Gọi AI để phân tích
            var prompt = AiRecommendPromptLibrary.BuildSubjectAnalysisPrompt(
                req.SubjectCode,
                req.SubjectName,
                req.Mark,
                dependents,
                req.CareerGoal);

            var aiResponse = await CompleteJsonChatAsync(
                AiRecommendPromptLibrary.SystemPrompt,
                prompt,
                ct);

            // 4. Parse response
            var parsed = TryParseSubjectAnalysisResponse(aiResponse);
            if (parsed == null)
            {
                // Fallback
                parsed = BuildSubjectAnalysisFallback(req, dependents);
            }

            // 5. Build dependent warnings
            var dependentWarnings = (dependents ?? new List<(string, string, int?)>())
                .Select(dep =>
                {
                    var semesterLabel = dep.Item3.HasValue && dep.Item3.Value > 0
                        ? $"Kỳ {dep.Item3.Value}"
                        : "các kỳ sau";
                    return new DependentSubjectWarning
                    {
                        SubjectCode = dep.Item1,
                        SubjectName = dep.Item2,
                        SemesterIndex = dep.Item3,
                        WarningMessage = $"Điểm thấp ở {req.SubjectName} ({req.SubjectCode}) có thể ảnh hưởng đến kết quả học tập của {dep.Item2} ({dep.Item1}) {semesterLabel}."
                    };
                })
                .ToList();

            return new SubjectAnalysisResponse
            {
                Success = true,
                Response = new SubjectAnalysisDto
                {
                    SubjectCode = req.SubjectCode,
                    SubjectName = req.SubjectName,
                    Mark = req.Mark,
                    ImprovementAnalysis = parsed,
                    DependentWarnings = dependentWarnings
                }
            };
        }

        private async Task<List<SubjectCur>> LoadCurriculumSubjectsForAnalysisAsync(
            string subjectCode,
            string? majorCode,
            CancellationToken ct)
        {
            var normalizedMajorCode = string.IsNullOrWhiteSpace(majorCode)
                ? string.Empty
                : NormalizeSubjectCode(majorCode);

            // Request tất cả subjects trong major để có thể tìm các môn phụ thuộc
            // Nếu không có majorCode, chỉ request môn hiện tại
            var eventRequest = new SubjectInfoEvent
            {
                MajorCode = normalizedMajorCode,
                // Nếu có majorCode, request tất cả subjects (để tìm dependents)
                // Nếu không, chỉ request môn hiện tại
                SubjectCodes = string.IsNullOrWhiteSpace(normalizedMajorCode)
                    ? new List<string> { subjectCode }
                    : new List<string>() // Empty list để lấy tất cả subjects trong major
            };

            try
            {
                var response = await subjectInfoClient.GetResponse<SubjectInfoEventResponse>(eventRequest, ct);
                var message = response.Message;

                if (message.Success && message.Response is { Count: > 0 } items)
                {
                    var allSubjects = items
                        .Select(item => new SubjectCur
                        {
                            SubjectCode = item.SubjectCode,
                            SubjectName = item.SubjectName,
                            Index = item.SemesterIndex.HasValue && item.SemesterIndex.Value > 0
                                ? item.SemesterIndex.Value
                                : item.SubjectIndex,
                            SubjectPrerequisiteCode = item.PrereqSubjectCodes ?? new List<string>()
                        })
                        .ToList();

                    // Nếu không có majorCode và không tìm thấy môn hiện tại, thêm vào
                    if (string.IsNullOrWhiteSpace(normalizedMajorCode))
                    {
                        var found = allSubjects.Any(s =>
                            s.SubjectCode.Equals(subjectCode, StringComparison.OrdinalIgnoreCase));
                        if (!found)
                        {
                            allSubjects.Add(new SubjectCur
                            {
                                SubjectCode = subjectCode,
                                SubjectName = string.Empty,
                                Index = 0,
                                SubjectPrerequisiteCode = new List<string>()
                            });
                        }
                    }

                    return allSubjects;
                }
            }
            catch
            {
                // Fallback
            }

            // Fallback: return môn hiện tại với empty prerequisites
            return new List<SubjectCur>
            {
                new SubjectCur
                {
                    SubjectCode = subjectCode,
                    SubjectName = string.Empty,
                    Index = 0,
                    SubjectPrerequisiteCode = new List<string>()
                }
            };
        }

        private static string? TryParseSubjectAnalysisResponse(string? jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return null;

            try
            {
                var doc = JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("improvementAnalysis", out var analysisElement))
                {
                    return analysisElement.GetString();
                }
            }
            catch
            {
                // Ignore parse errors
            }

            return null;
        }

        private static string BuildSubjectAnalysisFallback(
            SubjectAnalysisRequest req,
            List<(string subjectCode, string subjectName, int? semesterIndex)>? dependents)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## Phân tích điểm số {req.SubjectName}");
            sb.AppendLine();
            sb.AppendLine("### Đánh giá điểm số");
            sb.AppendLine($"- Điểm {req.Mark}/100 cho thấy mức độ nắm vững kiến thức cần được cải thiện.");
            sb.AppendLine();
            sb.AppendLine("### Điểm yếu cần cải thiện");
            sb.AppendLine("- Cần xác định các phần kiến thức còn yếu thông qua việc ôn tập lại.");
            sb.AppendLine();
            sb.AppendLine("### Lộ trình cải thiện 2-4 tuần");
            sb.AppendLine("- Ôn tập lại kiến thức cơ bản: 3-4 buổi/tuần");
            sb.AppendLine("- Làm bài tập và thực hành: 5-7 bài/tuần");
            sb.AppendLine("- Tham khảo tài liệu và video hướng dẫn");
            sb.AppendLine();
            sb.AppendLine("### Cảnh báo môn phụ thuộc");
            if (dependents != null && dependents.Count > 0)
            {
                foreach (var dep in dependents)
                {
                    var semesterLabel = dep.semesterIndex.HasValue && dep.semesterIndex.Value > 0
                        ? $"ở Kỳ {dep.semesterIndex.Value}"
                        : "trong các kỳ sau";
                    sb.AppendLine($"- {dep.subjectName} ({dep.subjectCode}) {semesterLabel}: Điểm thấp ở {req.SubjectName} có thể ảnh hưởng đến kết quả học tập môn này.");
                }
            }
            else
            {
                sb.AppendLine("- Không có môn học phụ thuộc trực tiếp.");
            }

            return sb.ToString();
        }

        private static Dictionary<string, List<(string subjectCode, string subjectName, int? semesterIndex)>> BuildDependentsLookupForAnalysis(IEnumerable<SubjectCur> subjects)
        {
            var map = new Dictionary<string, List<(string subjectCode, string subjectName, int? semesterIndex)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var subject in subjects ?? Enumerable.Empty<SubjectCur>())
            {
                foreach (var prerequisite in subject.SubjectPrerequisiteCode ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(prerequisite)) continue;

                    var normalizedPrereq = NormalizeSubjectCode(prerequisite);
                    if (string.IsNullOrWhiteSpace(normalizedPrereq)) continue;

                    if (!map.TryGetValue(normalizedPrereq, out var list))
                    {
                        list = new List<(string subjectCode, string subjectName, int? semesterIndex)>();
                        map[normalizedPrereq] = list;
                    }

                    var dependentName = string.IsNullOrWhiteSpace(subject.SubjectName)
                        ? subject.SubjectCode
                        : subject.SubjectName;
                    var semesterIndex = subject.Index > 0 ? subject.Index : (int?)null;

                    if (!list.Any(dep => dep.subjectCode.Equals(subject.SubjectCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add((subject.SubjectCode, dependentName, semesterIndex));
                    }
                }
            }

            return map;
        }
        #endregion

        #region Course Subject Analysis
        public async Task<SubjectAnalysisResponse> AnalyzeCourseSubjectAsync(Guid courseId, CancellationToken ct = default)
        {
            // 1. Lấy userId từ identity service
            var currentUser = identityService.GetCurrentUser();
            if (currentUser == null)
            {
                return new SubjectAnalysisResponse
                {
                    Success = false,
                    Response = new SubjectAnalysisDto
                    {
                        SubjectCode = string.Empty,
                        SubjectName = string.Empty,
                        Mark = 0,
                        ImprovementAnalysis = "Không tìm thấy thông tin người dùng."
                    }
                };
            }

            // 2. Lấy thông tin subject từ courseId qua messaging
            var courseInfoEvent = new GetInfoInternalCourseEvents(new List<Guid> { courseId }, currentUser.UserId);
            var courseInfoResponse = await getInfoInternalCourseClient.GetResponse<GetInfoInternalCourseResponse>(
                courseInfoEvent,
                ct);

            if (!courseInfoResponse.Message.Success || courseInfoResponse.Message.Response == null || courseInfoResponse.Message.Response.Count == 0)
            {
                return new SubjectAnalysisResponse
                {
                    Success = false,
                    Response = new SubjectAnalysisDto
                    {
                        SubjectCode = string.Empty,
                        SubjectName = string.Empty,
                        Mark = 0,
                        ImprovementAnalysis = "Không tìm thấy thông tin môn học cho khóa học này."
                    }
                };
            }

            var courseInfo = courseInfoResponse.Message.Response.FirstOrDefault(c => c.CourseId == courseId);
            if (courseInfo == null || string.IsNullOrWhiteSpace(courseInfo.SubjectCode))
            {
                return new SubjectAnalysisResponse
                {
                    Success = false,
                    Response = new SubjectAnalysisDto
                    {
                        SubjectCode = string.Empty,
                        SubjectName = string.Empty,
                        Mark = 0,
                        ImprovementAnalysis = "Không tìm thấy thông tin môn học cho khóa học này."
                    }
                };
            }

            // 3. Lấy điểm từ AI evaluation qua messaging
            var evaluationEvent = new GetOverviewAiEvaluationEvent(currentUser.UserId, courseId);
            var evaluationResponse = await overviewAiEvaluationClient.GetResponse<GetOverviewAiEvaluationEventResponse>(
                evaluationEvent,
                ct);

            if (!evaluationResponse.Message.Success)
            {
                return new SubjectAnalysisResponse
                {
                    Success = false,
                    Response = new SubjectAnalysisDto
                    {
                        SubjectCode = courseInfo.SubjectCode,
                        SubjectName = courseInfo.SubjectName ?? string.Empty,
                        Mark = 0,
                        ImprovementAnalysis = "Không tìm thấy dữ liệu đánh giá AI cho khóa học này."
                    }
                };
            }

            var mark = evaluationResponse.Message.Response.AverageScore100;

            // 4. Tạo SubjectAnalysisRequest và gọi AnalyzeSubjectMarkAsync
            var subjectAnalysisRequest = new SubjectAnalysisRequest
            {
                SubjectCode = courseInfo.SubjectCode,
                SubjectName = courseInfo.SubjectName ?? courseInfo.SubjectCode,
                Mark = mark,
                MajorCode = courseInfo.MajorCode,
                CareerGoal = null // Có thể thêm sau nếu cần
            };

            return await AnalyzeSubjectMarkAsync(subjectAnalysisRequest, ct);
        }
        #endregion

        #region Subject Mark Update Analysis
        public async Task<SubjectMarkUpdateResponse> AnalyzeSubjectMarkUpdateAsync(
            SubjectMarkUpdateRequest req,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(req);

            var normalizedSubjectCode = NormalizeSubjectCode(req.SubjectCode);
            if (string.IsNullOrWhiteSpace(normalizedSubjectCode))
            {
                return new SubjectMarkUpdateResponse
                {
                    Success = false,
                    Response = new SubjectMarkUpdateDto
                    {
                        SubjectCode = req.SubjectCode,
                        SubjectName = req.SubjectName,
                        OldMark = req.OldMark ?? 0,
                        NewMark = req.NewMark,
                        MarkImprovement = req.OldMark.HasValue ? req.NewMark - req.OldMark.Value : 0,
                        ImprovementAnalysis = "Mã môn học không hợp lệ.",
                        ComparisonAnalysis = "Mã môn học không hợp lệ."
                    }
                };
            }

            // 1. Lấy MajorCode từ SubjectCode thông qua SubjectInfoEvent (sử dụng v_major_semester_subject_prereqs)
            string? majorCode = null;
            try
            {
                var subjectInfoEvent = new SubjectInfoEvent
                {
                    MajorCode = string.Empty, // Empty để lấy tất cả majors có subject này
                    SubjectCodes = new List<string> { normalizedSubjectCode }
                };

                var subjectInfoResponse = await subjectInfoClient.GetResponse<SubjectInfoEventResponse>(
                    subjectInfoEvent,
                    ct);

                if (subjectInfoResponse.Message.Success &&
                    subjectInfoResponse.Message.Response != null &&
                    subjectInfoResponse.Message.Response.Count > 0)
                {
                    // Lấy MajorCode từ item đầu tiên
                    var firstItem = subjectInfoResponse.Message.Response.FirstOrDefault();
                    if (firstItem != null && !string.IsNullOrWhiteSpace(firstItem.MajorCode))
                    {
                        majorCode = firstItem.MajorCode;
                    }
                }
            }
            catch
            {
                // Nếu không lấy được từ SubjectInfoEvent, tiếp tục với majorCode = null
            }

            // 2. Load curriculum subjects để lấy thông tin về môn phụ thuộc
            var curriculumSubjects = await LoadCurriculumSubjectsForAnalysisAsync(
                normalizedSubjectCode,
                majorCode,
                ct);

            // 2. Tìm các môn phụ thuộc (subjects that depend on this subject)
            var dependentsLookup = BuildDependentsLookupForAnalysis(curriculumSubjects);
            dependentsLookup.TryGetValue(normalizedSubjectCode, out var dependents);

            // 3. Gọi AI để phân tích
            var prompt = AiRecommendPromptLibrary.BuildSubjectMarkUpdatePrompt(
                req.SubjectCode,
                req.SubjectName,
                req.OldMark,
                req.NewMark,
                req.NewAnalysis,
                dependents,
                req.CareerGoal);

            var aiResponse = await CompleteJsonChatAsync(
                AiRecommendPromptLibrary.SystemPrompt,
                prompt,
                ct);

            // 4. Parse response
            var parsed = TryParseSubjectMarkUpdateResponse(aiResponse);
            if (parsed == null)
            {
                // Fallback
                parsed = BuildSubjectMarkUpdateFallback(req, dependents);
            }

            // 5. Build dependent warnings - chỉ cảnh báo nếu điểm mới < 6
            var dependentWarnings = new List<DependentSubjectWarning>();
            if (req.NewMark < 60 && dependents != null && dependents.Count > 0)
            {
                dependentWarnings = dependents
                    .Select(dep =>
                    {
                        var semesterLabel = dep.Item3.HasValue && dep.Item3.Value > 0
                            ? $"Kỳ {dep.Item3.Value}"
                            : "các kỳ sau";
                        return new DependentSubjectWarning
                        {
                            SubjectCode = dep.Item1,
                            SubjectName = dep.Item2,
                            SemesterIndex = dep.Item3,
                            WarningMessage = $"⚠️ CẢNH BÁO: Điểm thấp ({req.NewMark}/100) ở {req.SubjectName} ({req.SubjectCode}) có thể ảnh hưởng nghiêm trọng đến kết quả học tập của {dep.Item2} ({dep.Item1}) {semesterLabel}. Cần củng cố kiến thức ngay lập tức."
                        };
                    })
                    .ToList();
            }

            var markImprovement = req.OldMark.HasValue ? req.NewMark - req.OldMark.Value : 0;

            return new SubjectMarkUpdateResponse
            {
                Success = true,
                Response = new SubjectMarkUpdateDto
                {
                    SubjectCode = req.SubjectCode,
                    SubjectName = req.SubjectName,
                    OldMark = req.OldMark ?? 0,
                    NewMark = req.NewMark,
                    MarkImprovement = markImprovement,
                    ImprovementAnalysis = parsed.Value.improvementAnalysis,
                    ComparisonAnalysis = parsed.Value.comparisonAnalysis,
                    DependentWarnings = dependentWarnings
                }
            };
        }

        private static (string improvementAnalysis, string comparisonAnalysis)? TryParseSubjectMarkUpdateResponse(string? jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return null;

            try
            {
                // Strip code fences nếu có
                var cleanedJson = AiQuizEvaluatorCommon.StripCodeFence(jsonResponse);

                var doc = JsonDocument.Parse(cleanedJson);
                string? improvementAnalysis = null;
                string? comparisonAnalysis = null;

                if (doc.RootElement.TryGetProperty("improvementAnalysis", out var improvementElement))
                {
                    improvementAnalysis = improvementElement.GetString();
                    // Clean markdown: unescape và strip code fences nếu có
                    if (!string.IsNullOrWhiteSpace(improvementAnalysis))
                    {
                        improvementAnalysis = CleanMarkdown(improvementAnalysis);
                    }
                }

                if (doc.RootElement.TryGetProperty("comparisonAnalysis", out var comparisonElement))
                {
                    comparisonAnalysis = comparisonElement.GetString();
                    // Clean markdown: unescape và strip code fences nếu có
                    if (!string.IsNullOrWhiteSpace(comparisonAnalysis))
                    {
                        comparisonAnalysis = CleanMarkdown(comparisonAnalysis);
                    }
                }

                if (string.IsNullOrWhiteSpace(improvementAnalysis) && string.IsNullOrWhiteSpace(comparisonAnalysis))
                {
                    return null;
                }

                return (
                    improvementAnalysis ?? string.Empty,
                    comparisonAnalysis ?? string.Empty
                );
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractSectionFromMarkdown(string markdown, string sectionTitle)
        {
            if (string.IsNullOrWhiteSpace(markdown) || string.IsNullOrWhiteSpace(sectionTitle))
                return string.Empty;

            // Tìm section bằng heading (## hoặc ###)
            var patterns = new[]
            {
                $"## {sectionTitle}",
                $"### {sectionTitle}",
                $"##{sectionTitle}",
                $"###{sectionTitle}"
            };

            foreach (var pattern in patterns)
            {
                var index = markdown.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    // Tìm vị trí bắt đầu nội dung (sau heading và newline)
                    var contentStart = markdown.IndexOf('\n', index);
                    if (contentStart < 0) continue;
                    contentStart++; // Bỏ qua newline

                    // Tìm heading tiếp theo (## hoặc ###) hoặc kết thúc document
                    var nextHeading = markdown.IndexOf("\n##", contentStart, StringComparison.Ordinal);
                    if (nextHeading < 0)
                        nextHeading = markdown.Length;

                    var content = markdown.Substring(contentStart, nextHeading - contentStart).Trim();
                    return content;
                }
            }

            return string.Empty;
        }

        private static List<string> ExtractBullets(string text)
        {
            var bullets = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return bullets;

            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Tìm các bullet points (bắt đầu bằng - hoặc •)
                if (trimmed.StartsWith("-", StringComparison.Ordinal) || trimmed.StartsWith("•", StringComparison.Ordinal))
                {
                    var content = trimmed.Substring(1).Trim();
                    if (!string.IsNullOrWhiteSpace(content) && content.Length > 5) // Bỏ qua bullet quá ngắn
                    {
                        bullets.Add(content);
                    }
                }
            }

            return bullets;
        }

        private static string CleanMarkdown(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return markdown;

            // Strip code fences nếu có
            var cleaned = AiQuizEvaluatorCommon.StripCodeFence(markdown);

            // Unescape các ký tự escape sequence
            cleaned = cleaned.Replace("\\n", "\n", StringComparison.Ordinal);
            cleaned = cleaned.Replace("\\t", "\t", StringComparison.Ordinal);
            cleaned = cleaned.Replace("\\r", "\r", StringComparison.Ordinal);
            cleaned = cleaned.Replace("\\\"", "\"", StringComparison.Ordinal);
            cleaned = cleaned.Replace("\\\\", "\\", StringComparison.Ordinal);

            // Loại bỏ các dòng trống thừa ở đầu và cuối
            cleaned = cleaned.Trim();

            // Đảm bảo format markdown đúng: các heading phải có dòng trống trước đó (trừ dòng đầu)
            var lines = cleaned.Split('\n').ToList();
            var result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                // Nếu là heading (## hoặc ###) và không phải dòng đầu
                if ((trimmedLine.StartsWith("##", StringComparison.Ordinal) ||
                     trimmedLine.StartsWith("###", StringComparison.Ordinal)) &&
                    result.Count > 0 &&
                    !string.IsNullOrWhiteSpace(result[result.Count - 1]))
                {
                    // Thêm dòng trống trước heading
                    result.Add(string.Empty);
                }

                result.Add(line);
            }

            return string.Join("\n", result).Trim();
        }

        private static (string improvementAnalysis, string comparisonAnalysis) BuildSubjectMarkUpdateFallback(
            SubjectMarkUpdateRequest req,
            List<(string subjectCode, string subjectName, int? semesterIndex)>? dependents = null)
        {
            var hasOldMark = req.OldMark.HasValue;
            var markImprovement = hasOldMark ? req.NewMark - req.OldMark.Value : 0;
            var improvementPercentage = hasOldMark && req.OldMark.Value > 0 ? (markImprovement / req.OldMark.Value) * 100 : 0;

            var improvementSb = new StringBuilder();
            improvementSb.AppendLine($"## Phân tích cải thiện điểm số {req.SubjectName}");
            improvementSb.AppendLine();
            improvementSb.AppendLine("### Mức độ cải thiện");

            // Phân loại theo điểm mới
            if (req.NewMark < 60.0)
            {
                if (hasOldMark)
                {
                    if (markImprovement > 0)
                    {
                        improvementSb.AppendLine($"- Điểm đã cải thiện **{markImprovement:F1} điểm** ({improvementPercentage:F1}%) từ {req.OldMark.Value}/100 lên {req.NewMark}/100.");
                        improvementSb.AppendLine("- ⚠️ CẢNH BÁO: Mặc dù có cải thiện, điểm hiện tại vẫn ở mức RẤT YẾU (< 60), có nguy cơ rớt môn.");
                    }
                    else if (markImprovement < 0)
                    {
                        improvementSb.AppendLine($"- Điểm đã giảm **{Math.Abs(markImprovement):F1} điểm** ({Math.Abs(improvementPercentage):F1}%) từ {req.OldMark.Value}/100 xuống {req.NewMark}/100.");
                        improvementSb.AppendLine("- ⚠️ CẢNH BÁO NGHIÊM TRỌNG: Điểm hiện tại ở mức RẤT YẾU (< 60), có nguy cơ rớt môn.");
                    }
                    else
                    {
                        improvementSb.AppendLine($"- Điểm số giữ nguyên ở mức {req.OldMark.Value}/100.");
                        improvementSb.AppendLine("- ⚠️ CẢNH BÁO: Điểm hiện tại ở mức RẤT YẾU (< 60), cần cải thiện ngay lập tức.");
                    }
                }
                else
                {
                    improvementSb.AppendLine($"- Điểm hiện tại là {req.NewMark}/100 (không có điểm cũ để so sánh).");
                    improvementSb.AppendLine("- ⚠️ CẢNH BÁO: Điểm hiện tại ở mức RẤT YẾU (< 60), có nguy cơ rớt môn.");
                }
            }
            else if (req.NewMark >= 6.0 && req.NewMark < 7.0)
            {
                if (markImprovement > 0)
                {
                    improvementSb.AppendLine($"- Điểm đã cải thiện **{markImprovement:F1} điểm** ({improvementPercentage:F1}%) từ {req.OldMark}/10 lên {req.NewMark}/10.");
                    improvementSb.AppendLine("- Đây là sự cải thiện tích cực, nhưng điểm vẫn ở mức trung bình yếu, cần tiếp tục cải thiện.");
                }
                else if (markImprovement < 0)
                {
                    improvementSb.AppendLine($"- Điểm đã giảm **{Math.Abs(markImprovement):F1} điểm** ({Math.Abs(improvementPercentage):F1}%) từ {req.OldMark}/10 xuống {req.NewMark}/10.");
                    improvementSb.AppendLine("- Cần xem xét lại phương pháp học tập và tập trung vào các phần kiến thức còn yếu.");
                }
                else
                {
                    improvementSb.AppendLine($"- Điểm số giữ nguyên ở mức {req.OldMark}/10.");
                    improvementSb.AppendLine("- Cần thay đổi phương pháp học tập để đạt kết quả tốt hơn.");
                }
            }
            else if (req.NewMark >= 7.0 && req.NewMark < 8.0)
            {
                if (markImprovement > 0)
                {
                    improvementSb.AppendLine($"- Điểm đã cải thiện **{markImprovement:F1} điểm** ({improvementPercentage:F1}%) từ {req.OldMark}/10 lên {req.NewMark}/10.");
                    improvementSb.AppendLine("- Khen ngợi: Bạn đã đạt mức khá tốt, đây là sự cải thiện tích cực.");
                }
                else if (markImprovement < 0)
                {
                    improvementSb.AppendLine($"- Điểm đã giảm **{Math.Abs(markImprovement):F1} điểm** ({Math.Abs(improvementPercentage):F1}%) từ {req.OldMark}/10 xuống {req.NewMark}/10.");
                    improvementSb.AppendLine("- Mặc dù điểm giảm, bạn vẫn ở mức khá, cần duy trì và cải thiện lại.");
                }
                else
                {
                    improvementSb.AppendLine($"- Điểm số giữ nguyên ở mức {req.OldMark}/10.");
                    improvementSb.AppendLine("- Bạn đang ở mức khá, có thể phát triển thêm để đạt mức tốt hơn.");
                }
            }
            else // >= 8.0
            {
                if (markImprovement > 0)
                {
                    improvementSb.AppendLine($"- Điểm đã cải thiện **{markImprovement:F1} điểm** ({improvementPercentage:F1}%) từ {req.OldMark}/10 lên {req.NewMark}/10.");
                    improvementSb.AppendLine("- 🎉 Khen ngợi: Bạn đã nắm vững kiến thức rất tốt, đây là sự cải thiện xuất sắc!");
                }
                else if (markImprovement < 0)
                {
                    improvementSb.AppendLine($"- Điểm đã giảm **{Math.Abs(markImprovement):F1} điểm** ({Math.Abs(improvementPercentage):F1}%) từ {req.OldMark}/10 xuống {req.NewMark}/10.");
                    improvementSb.AppendLine("- Mặc dù điểm giảm, bạn vẫn ở mức xuất sắc, cần duy trì phong độ.");
                }
                else
                {
                    improvementSb.AppendLine($"- Điểm số giữ nguyên ở mức {req.OldMark}/10.");
                    improvementSb.AppendLine("- Khen ngợi: Bạn đang ở mức xuất sắc, tiếp tục phát triển để duy trì phong độ.");
                }
            }

            improvementSb.AppendLine();
            if (hasOldMark)
            {
                improvementSb.AppendLine("### So sánh điểm cũ và mới");
                improvementSb.AppendLine($"- Điểm cũ ({req.OldMark.Value}/100): {ClassifyMarkLevel(req.OldMark.Value / 10)}");
                improvementSb.AppendLine($"- Điểm mới ({req.NewMark}/100): {ClassifyMarkLevel(req.NewMark / 10)}");
            }
            else
            {
                improvementSb.AppendLine("### Điểm số hiện tại");
                improvementSb.AppendLine($"- Điểm hiện tại ({req.NewMark}/100): {ClassifyMarkLevel(req.NewMark / 10)}");
            }
            improvementSb.AppendLine();
            improvementSb.AppendLine("### Nguyên nhân cải thiện");
            if (!string.IsNullOrWhiteSpace(req.NewAnalysis))
            {
                improvementSb.AppendLine($"- Dựa trên phân tích: {req.NewAnalysis}");
            }
            else
            {
                improvementSb.AppendLine("- Chưa có phân tích chi tiết về nguyên nhân cải thiện.");
            }
            improvementSb.AppendLine();

            // Cảnh báo/Nhận xét về môn phụ thuộc
            improvementSb.AppendLine("### Cảnh báo/Nhận xét về môn phụ thuộc ở kỳ tiếp theo");
            if (dependents != null && dependents.Count > 0)
            {
                if (req.NewMark < 6.0)
                {
                    foreach (var dep in dependents)
                    {
                        var semesterLabel = dep.semesterIndex.HasValue && dep.semesterIndex.Value > 0
                            ? $"Kỳ {dep.semesterIndex.Value}"
                            : "các kỳ sau";
                        improvementSb.AppendLine($"- ⚠️ **CẢNH BÁO NGHIÊM TRỌNG**: Điểm thấp ({req.NewMark}/10) ở {req.SubjectName} có thể khiến bạn RỚT môn {dep.subjectName} ({dep.subjectCode}) {semesterLabel}. Cần củng cố kiến thức ngay lập tức.");
                    }
                }
                else if (req.NewMark >= 6.0 && req.NewMark < 7.0)
                {
                    foreach (var dep in dependents)
                    {
                        var semesterLabel = dep.semesterIndex.HasValue && dep.semesterIndex.Value > 0
                            ? $"Kỳ {dep.semesterIndex.Value}"
                            : "các kỳ sau";
                        improvementSb.AppendLine($"- ⚠️ **Lưu ý**: Điểm trung bình yếu ({req.NewMark}/10) ở {req.SubjectName} có thể ảnh hưởng đến kết quả học tập của {dep.subjectName} ({dep.subjectCode}) {semesterLabel}. Nên củng cố thêm.");
                    }
                }
                else if (req.NewMark >= 7.0 && req.NewMark < 8.0)
                {
                    foreach (var dep in dependents)
                    {
                        var semesterLabel = dep.semesterIndex.HasValue && dep.semesterIndex.Value > 0
                            ? $"Kỳ {dep.semesterIndex.Value}"
                            : "các kỳ sau";
                        improvementSb.AppendLine($"- ✅ **Nhận xét tích cực**: Với điểm khá ({req.NewMark}/10) ở {req.SubjectName}, bạn có khả năng học tốt môn {dep.subjectName} ({dep.subjectCode}) {semesterLabel}. Duy trì phong độ.");
                    }
                }
                else // >= 8.0
                {
                    foreach (var dep in dependents)
                    {
                        var semesterLabel = dep.semesterIndex.HasValue && dep.semesterIndex.Value > 0
                            ? $"Kỳ {dep.semesterIndex.Value}"
                            : "các kỳ sau";
                        improvementSb.AppendLine($"- 🎉 **Nhận xét rất tích cực**: Với điểm xuất sắc ({req.NewMark}/10) ở {req.SubjectName}, bạn có khả năng học rất tốt môn {dep.subjectName} ({dep.subjectCode}) {semesterLabel}. Tiếp tục phát huy!");
                    }
                }
            }
            else
            {
                improvementSb.AppendLine("- Không có môn học phụ thuộc trực tiếp.");
            }

            improvementSb.AppendLine();
            improvementSb.AppendLine("### Đề xuất hành động tiếp theo");

            // Phân loại lộ trình theo điểm mới
            if (req.NewMark < 6.0)
            {
                improvementSb.AppendLine("- Lộ trình cải thiện KHẨN CẤP: 5-7 buổi/tuần, 10+ bài tập/tuần.");
                improvementSb.AppendLine("- Ôn lại toàn bộ kiến thức cơ bản và làm lại bài tập đã sai.");
                improvementSb.AppendLine("- Nhờ mentor/giáo viên hỗ trợ giải đáp thắc mắc ngay lập tức.");
            }
            else if (req.NewMark >= 6.0 && req.NewMark < 7.0)
            {
                improvementSb.AppendLine("- Lộ trình cải thiện vừa phải: 4-5 buổi/tuần, 7-9 bài tập/tuần.");
                improvementSb.AppendLine("- Củng cố các phần kiến thức còn yếu và làm thêm bài tập.");
            }
            else if (req.NewMark >= 7.0 && req.NewMark < 8.0)
            {
                improvementSb.AppendLine("- Lộ trình phát triển vừa phải: 3-4 buổi/tuần, 5-7 bài tập/tuần.");
                improvementSb.AppendLine("- Duy trì phương pháp học tập hiện tại và phát triển thêm các phần nâng cao.");
            }
            else // >= 8.0
            {
                improvementSb.AppendLine("- Lộ trình phát triển nâng cao: 2-3 buổi/tuần, 3-5 bài tập nâng cao/tuần.");
                improvementSb.AppendLine("- Tiếp tục phát triển các phần kiến thức chuyên sâu và thử thách bản thân với bài tập khó hơn.");
            }

            // Parse newAnalysis để lấy thông tin về điểm mạnh, điểm yếu
            var strengths = ExtractSectionFromMarkdown(req.NewAnalysis, "Điểm mạnh nổi bật");
            var weaknesses = ExtractSectionFromMarkdown(req.NewAnalysis, "Vấn đề & Khoảng trống kỹ năng");
            var rootCauses = ExtractSectionFromMarkdown(req.NewAnalysis, "Nguyên nhân gốc");
            var priorities = ExtractSectionFromMarkdown(req.NewAnalysis, "Ưu tiên hành động");
            var trends = ExtractSectionFromMarkdown(req.NewAnalysis, "Xu hướng theo thời gian");

            var comparisonSb = new StringBuilder();
            comparisonSb.AppendLine("## So sánh chi tiết");
            comparisonSb.AppendLine();

            // Điểm cũ - viết dài hơn, chi tiết hơn, sử dụng dữ liệu từ newAnalysis
            // Chỉ hiển thị nếu có OldMark
            if (hasOldMark)
            {
                var oldMarkInScale10 = req.OldMark.Value / 10;
                comparisonSb.AppendLine($"### Điểm cũ ({req.OldMark.Value}/100)");
                var oldLevel = ClassifyMarkLevel(oldMarkInScale10);
                var oldAssessment = GetMarkAssessment(oldMarkInScale10);
                comparisonSb.AppendLine($"Điểm {req.OldMark.Value}/100 cho thấy mức độ nắm vững kiến thức ở mức {oldLevel.ToLower()}. {oldAssessment}");
            }
            else
            {
                comparisonSb.AppendLine("### Điểm cũ");
                comparisonSb.AppendLine("Không có điểm cũ để so sánh.");
            }

            // Sử dụng dữ liệu từ newAnalysis nếu có
            if (!string.IsNullOrWhiteSpace(strengths))
            {
                var strengthBullets = ExtractBullets(strengths);
                if (strengthBullets.Count > 0)
                {
                    comparisonSb.AppendLine($"Dựa trên phân tích, điểm mạnh của bạn ở thời điểm này bao gồm: {string.Join(", ", strengthBullets.Take(2))}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(weaknesses))
            {
                var weaknessBullets = ExtractBullets(weaknesses);
                if (weaknessBullets.Count > 0)
                {
                    comparisonSb.AppendLine($"Điểm yếu chính được xác định là: {string.Join(", ", weaknessBullets.Take(2))}.");
                }
            }
            else
            {
                // Fallback nếu không có dữ liệu từ newAnalysis
                if (hasOldMark)
                {
                    var oldMarkInScale10 = req.OldMark.Value / 10;
                    if (oldMarkInScale10 < 6.0)
                    {
                        comparisonSb.AppendLine($"Với điểm này, bạn đang gặp khó khăn trong việc nắm vững các kiến thức cơ bản của môn học. Các phần kiến thức nền tảng như khái niệm, nguyên lý cơ bản và cách áp dụng vào bài tập đơn giản có thể chưa được nắm vững.");
                        comparisonSb.AppendLine($"Điểm yếu chính có thể nằm ở việc chưa hiểu sâu các khái niệm cốt lõi, thiếu kỹ năng giải quyết bài tập cơ bản, hoặc chưa có phương pháp học tập hiệu quả.");
                    }
                    else if (oldMarkInScale10 >= 6.0 && oldMarkInScale10 < 7.0)
                    {
                        comparisonSb.AppendLine($"Với điểm này, bạn đã nắm được một phần kiến thức cơ bản nhưng vẫn còn nhiều khoảng trống. Các phần kiến thức trọng tâm có thể đã được tiếp thu nhưng chưa đủ sâu để áp dụng vào các bài tập phức tạp hơn.");
                    }
                }
            }
            comparisonSb.AppendLine();

            // Điểm mới - viết dài hơn, chi tiết hơn, sử dụng dữ liệu từ newAnalysis
            var newMarkInScale10 = req.NewMark / 10;
            comparisonSb.AppendLine($"### Điểm mới ({req.NewMark}/100)");
            var newLevel = ClassifyMarkLevel(newMarkInScale10);
            var newAssessment = GetMarkAssessment(newMarkInScale10);
            comparisonSb.AppendLine($"Điểm {req.NewMark}/100 cho thấy mức độ nắm vững kiến thức hiện tại ở mức {newLevel.ToLower()}. {newAssessment}");

            // Sử dụng dữ liệu từ newAnalysis
            if (!string.IsNullOrWhiteSpace(strengths))
            {
                var strengthBullets = ExtractBullets(strengths);
                if (strengthBullets.Count > 0)
                {
                    comparisonSb.AppendLine($"Điểm mạnh hiện tại của bạn, dựa trên phân tích chi tiết, bao gồm: {string.Join(", ", strengthBullets)}. Đây là những điểm tích cực cần được duy trì và phát triển thêm.");
                }
            }

            if (!string.IsNullOrWhiteSpace(weaknesses))
            {
                var weaknessBullets = ExtractBullets(weaknesses);
                if (weaknessBullets.Count > 0)
                {
                    comparisonSb.AppendLine($"Điểm yếu và các vấn đề cần cải thiện được xác định cụ thể là: {string.Join(", ", weaknessBullets)}. Đây là những phần kiến thức và kỹ năng cần được tập trung củng cố để đạt được kết quả tốt hơn.");
                }
            }
            else
            {
                // Fallback nếu không có dữ liệu từ newAnalysis
                if (newMarkInScale10 < 6.0)
                {
                    comparisonSb.AppendLine($"Với điểm này, bạn vẫn đang gặp khó khăn trong việc nắm vững các kiến thức cơ bản. Các phần kiến thức nền tảng vẫn cần được củng cố thêm.");
                }
            }
            comparisonSb.AppendLine();

            // So sánh chi tiết - sử dụng dữ liệu từ newAnalysis
            comparisonSb.AppendLine("### So sánh chi tiết");
            if (hasOldMark && markImprovement > 0)
            {
                var oldMarkInScale10 = req.OldMark.Value / 10;
                comparisonSb.AppendLine($"Sự cải thiện {markImprovement:F1} điểm ({improvementPercentage:F1}%) cho thấy bạn đã có sự tiến bộ rõ rệt trong quá trình học tập.");
                if (oldMarkInScale10 < 6.0 && newMarkInScale10 >= 6.0)
                {
                    comparisonSb.AppendLine($"Bạn đã vượt qua ngưỡng tối thiểu (60/100), từ mức yếu lên mức trung bình, đây là một bước tiến quan trọng. Điều này cho thấy bạn đã nỗ lực củng cố kiến thức nền tảng và có phương pháp học tập hiệu quả hơn.");
                }
                else if (oldMarkInScale10 < 7.0 && newMarkInScale10 >= 7.0)
                {
                    comparisonSb.AppendLine($"Bạn đã vượt qua ngưỡng khá (70/100), từ mức trung bình lên mức khá, thể hiện sự cải thiện đáng kể. Điều này cho thấy bạn không chỉ nắm vững kiến thức cơ bản mà còn có khả năng áp dụng vào các bài tập phức tạp hơn.");
                }
                else if (oldMarkInScale10 < 8.0 && newMarkInScale10 >= 8.0)
                {
                    comparisonSb.AppendLine($"Bạn đã vượt qua ngưỡng xuất sắc (80/100), từ mức khá lên mức xuất sắc, đây là một thành tựu đáng khen ngợi. Điều này cho thấy bạn đã nắm vững kiến thức một cách sâu sắc và có khả năng vận dụng linh hoạt.");
                }
                else
                {
                    comparisonSb.AppendLine($"Mặc dù chưa vượt qua ngưỡng mới, nhưng sự cải thiện này cho thấy bạn đang đi đúng hướng. Các phần kiến thức đã được củng cố và bạn đang tiến gần hơn đến mức cao hơn.");
                }

                // Sử dụng dữ liệu từ newAnalysis
                if (!string.IsNullOrWhiteSpace(strengths))
                {
                    var strengthBullets = ExtractBullets(strengths);
                    if (strengthBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Những phần đã cải thiện rõ rệt, dựa trên phân tích chi tiết, bao gồm: {string.Join(", ", strengthBullets)}. Đây là những điểm tích cực cho thấy bạn đang đi đúng hướng.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(weaknesses))
                {
                    var weaknessBullets = ExtractBullets(weaknesses);
                    if (weaknessBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Tuy nhiên, vẫn còn một số phần cần chú ý để đạt được mức cao hơn: {string.Join(", ", weaknessBullets)}. Đây là những vấn đề cần được tập trung cải thiện trong thời gian tới.");
                    }
                }
            }
            else if (hasOldMark && markImprovement < 0)
            {
                comparisonSb.AppendLine($"Sự giảm {Math.Abs(markImprovement):F1} điểm ({Math.Abs(improvementPercentage):F1}%) cho thấy bạn cần chú ý và điều chỉnh phương pháp học tập.");

                // Sử dụng dữ liệu từ newAnalysis về nguyên nhân
                if (!string.IsNullOrWhiteSpace(rootCauses))
                {
                    var causeBullets = ExtractBullets(rootCauses);
                    if (causeBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Nguyên nhân gốc của sự giảm điểm, dựa trên phân tích chi tiết, bao gồm: {string.Join(", ", causeBullets)}. Đây là những vấn đề cần được giải quyết để cải thiện điểm số.");
                    }
                }
                else
                {
                    comparisonSb.AppendLine($"Điều này có thể do nhiều nguyên nhân: chưa ôn tập đầy đủ, gặp khó khăn với các phần kiến thức mới, hoặc phương pháp học tập chưa phù hợp. Cần xác định nguyên nhân cụ thể để có biện pháp khắc phục.");
                }

                if (newMarkInScale10 < 6.0)
                {
                    comparisonSb.AppendLine($"Điểm hiện tại đã xuống dưới ngưỡng tối thiểu (60/100), đây là dấu hiệu cảnh báo. Cần củng cố lại kiến thức nền tảng ngay lập tức và tìm kiếm sự hỗ trợ từ giáo viên hoặc bạn học.");
                }
                else
                {
                    comparisonSb.AppendLine($"Mặc dù điểm đã giảm, nhưng vẫn ở trên ngưỡng tối thiểu. Cần điều chỉnh phương pháp học tập và tập trung vào các phần kiến thức còn yếu để cải thiện lại điểm số.");
                }
            }
            else if (hasOldMark)
            {
                comparisonSb.AppendLine($"Điểm số giữ nguyên cho thấy bạn cần thay đổi cách tiếp cận để đạt kết quả tốt hơn.");

                // Sử dụng dữ liệu từ newAnalysis về nguyên nhân
                if (!string.IsNullOrWhiteSpace(rootCauses))
                {
                    var causeBullets = ExtractBullets(rootCauses);
                    if (causeBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Nguyên nhân khiến điểm số không cải thiện, dựa trên phân tích, bao gồm: {string.Join(", ", causeBullets)}. Đây là những vấn đề cần được giải quyết để đạt được sự cải thiện.");
                    }
                }
                else
                {
                    comparisonSb.AppendLine($"Mặc dù đã có nỗ lực, nhưng điểm số không cải thiện có thể do: phương pháp học tập chưa hiệu quả, chưa tập trung vào đúng các phần kiến thức cần thiết, hoặc cần thêm thời gian để kiến thức được củng cố.");
                }

                if (newMarkInScale10 < 6.0)
                {
                    comparisonSb.AppendLine($"Điểm hiện tại vẫn ở dưới ngưỡng tối thiểu (60/100), cần có biện pháp khẩn cấp để cải thiện. Nên tham khảo ý kiến của giáo viên hoặc tìm kiếm các nguồn tài liệu học tập khác.");
                }
                else
                {
                    comparisonSb.AppendLine($"Cần điều chỉnh phương pháp học tập, có thể thử các cách tiếp cận mới như học nhóm, làm thêm bài tập, hoặc xem lại các phần kiến thức cơ bản.");
                }
            }
            else
            {
                // Không có OldMark để so sánh
                comparisonSb.AppendLine($"Điểm số hiện tại là {req.NewMark}/100. Dựa trên phân tích, bạn cần tiếp tục cải thiện để đạt kết quả tốt hơn.");
            }
            comparisonSb.AppendLine();

            // Xu hướng học tập - sử dụng dữ liệu từ newAnalysis
            comparisonSb.AppendLine("### Xu hướng học tập");

            // Sử dụng dữ liệu từ newAnalysis về xu hướng nếu có
            if (!string.IsNullOrWhiteSpace(trends) && !trends.Trim().Equals("—", StringComparison.OrdinalIgnoreCase))
            {
                comparisonSb.AppendLine($"Dựa trên phân tích xu hướng theo thời gian: {trends.Trim()}");
            }

            if (hasOldMark && markImprovement > 0)
            {
                if (newMarkInScale10 >= 8.0)
                {
                    comparisonSb.AppendLine($"Xu hướng hiện tại cho thấy bạn đang cải thiện tích cực và đạt mức xuất sắc. Nếu duy trì phương pháp học tập hiện tại, bạn có thể tiếp tục phát triển và đạt được những thành tích cao hơn.");
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu tiếp tục duy trì nhịp độ học tập và phương pháp hiện tại, bạn có thể giữ vững hoặc cải thiện thêm điểm số trong các bài kiểm tra tiếp theo.");
                }
                else if (newMarkInScale10 >= 7.0)
                {
                    comparisonSb.AppendLine($"Xu hướng hiện tại cho thấy bạn đang cải thiện tích cực và đạt mức khá tốt. Nếu duy trì phương pháp học tập hiện tại, bạn có thể tiếp tục cải thiện và đạt mức xuất sắc.");
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu tiếp tục nỗ lực và củng cố các phần kiến thức còn yếu, bạn có thể đạt được điểm số cao hơn trong các bài kiểm tra tiếp theo.");
                }
                else
                {
                    comparisonSb.AppendLine($"Xu hướng hiện tại cho thấy bạn đang cải thiện tích cực, mặc dù vẫn cần tiếp tục nỗ lực. Nếu duy trì phương pháp học tập hiện tại và tăng cường luyện tập, bạn có thể tiếp tục cải thiện điểm số.");
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu tiếp tục nỗ lực và tập trung vào các phần kiến thức cơ bản, bạn có thể đạt được điểm số tốt hơn và vượt qua các ngưỡng tiếp theo.");
                }
            }
            else if (markImprovement < 0)
            {
                comparisonSb.AppendLine($"Xu hướng hiện tại cho thấy điểm số đang giảm, đây là dấu hiệu cần chú ý. Cần điều chỉnh phương pháp học tập và tập trung vào các phần kiến thức còn yếu.");
                if (req.NewMark < 6.0)
                {
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu không có biện pháp khắc phục ngay lập tức, điểm số có thể tiếp tục giảm và ảnh hưởng đến kết quả học tập tổng thể. Cần có kế hoạch học tập cụ thể và tìm kiếm sự hỗ trợ.");
                }
                else
                {
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu điều chỉnh phương pháp học tập và tập trung vào các phần kiến thức còn yếu, bạn có thể cải thiện lại điểm số và đạt được kết quả tốt hơn.");
                }
            }
            else
            {
                comparisonSb.AppendLine($"Xu hướng hiện tại cho thấy điểm số đang ổn định, nhưng chưa có sự cải thiện. Cần thay đổi phương pháp học tập để đạt được kết quả tốt hơn.");
                if (req.NewMark < 6.0)
                {
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu không có thay đổi trong phương pháp học tập, điểm số có thể tiếp tục ở mức thấp. Cần có kế hoạch học tập mới và tìm kiếm các nguồn tài liệu học tập hiệu quả hơn.");
                }
                else
                {
                    comparisonSb.AppendLine($"Dự đoán xu hướng tiếp theo: Nếu thay đổi phương pháp học tập và tập trung vào các phần kiến thức cần cải thiện, bạn có thể đạt được điểm số cao hơn trong các bài kiểm tra tiếp theo.");
                }
            }
            comparisonSb.AppendLine();

            // Ý nghĩa của sự thay đổi - sử dụng dữ liệu từ newAnalysis
            comparisonSb.AppendLine("### Ý nghĩa của sự thay đổi");
            if (markImprovement > 0)
            {
                comparisonSb.AppendLine($"Sự cải thiện này có ý nghĩa quan trọng đối với quá trình học tập của bạn. Nó cho thấy bạn đã tìm được phương pháp học tập phù hợp và đang đi đúng hướng.");
                comparisonSb.AppendLine($"Đối với các môn học liên quan, việc cải thiện điểm số ở môn này sẽ tạo nền tảng tốt hơn để học các môn phụ thuộc. Kiến thức đã được củng cố sẽ giúp bạn tiếp thu các kiến thức mới dễ dàng hơn.");

                // Sử dụng dữ liệu từ newAnalysis về ưu tiên hành động
                if (!string.IsNullOrWhiteSpace(priorities))
                {
                    var priorityBullets = ExtractBullets(priorities);
                    if (priorityBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Để duy trì và phát triển thêm, bạn nên tập trung vào các ưu tiên hành động: {string.Join(", ", priorityBullets.Take(3))}. Điều này sẽ giúp bạn tiếp tục cải thiện và đạt được kết quả tốt hơn.");
                    }
                }
                else
                {
                    comparisonSb.AppendLine($"Điều này cũng cho thấy bạn có khả năng cải thiện và phát triển, điều quan trọng là duy trì động lực và tiếp tục nỗ lực.");
                }
            }
            else if (markImprovement < 0)
            {
                comparisonSb.AppendLine($"Sự giảm điểm này là một dấu hiệu cảnh báo về quá trình học tập của bạn. Nó cho thấy có thể có vấn đề với phương pháp học tập hoặc việc tiếp thu kiến thức.");
                comparisonSb.AppendLine($"Đối với các môn học liên quan, việc điểm số giảm ở môn này có thể ảnh hưởng đến khả năng học các môn phụ thuộc. Cần củng cố lại kiến thức nền tảng để tránh ảnh hưởng đến các môn học khác.");

                // Sử dụng dữ liệu từ newAnalysis về ưu tiên hành động
                if (!string.IsNullOrWhiteSpace(priorities))
                {
                    var priorityBullets = ExtractBullets(priorities);
                    if (priorityBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Để khắc phục tình trạng này, bạn cần tập trung vào các ưu tiên hành động: {string.Join(", ", priorityBullets.Take(3))}. Đây là những bước cụ thể cần thực hiện để cải thiện điểm số.");
                    }
                }
                else
                {
                    comparisonSb.AppendLine($"Tuy nhiên, đây cũng là cơ hội để bạn nhìn nhận lại và điều chỉnh phương pháp học tập, tìm ra cách tiếp cận phù hợp hơn.");
                }
            }
            else
            {
                comparisonSb.AppendLine($"Việc điểm số giữ nguyên cho thấy bạn cần thay đổi cách tiếp cận để đạt được sự cải thiện. Mặc dù đã có nỗ lực, nhưng phương pháp hiện tại có thể chưa phù hợp.");
                comparisonSb.AppendLine($"Đối với các môn học liên quan, việc điểm số không cải thiện có thể ảnh hưởng đến khả năng học các môn phụ thuộc. Cần tìm cách củng cố kiến thức để tạo nền tảng tốt hơn.");

                // Sử dụng dữ liệu từ newAnalysis về ưu tiên hành động
                if (!string.IsNullOrWhiteSpace(priorities))
                {
                    var priorityBullets = ExtractBullets(priorities);
                    if (priorityBullets.Count > 0)
                    {
                        comparisonSb.AppendLine($"Để đạt được sự cải thiện, bạn nên tập trung vào các ưu tiên hành động: {string.Join(", ", priorityBullets.Take(3))}. Đây là những bước cụ thể cần thực hiện để thay đổi kết quả học tập.");
                    }
                }
                else
                {
                    comparisonSb.AppendLine($"Đây là thời điểm thích hợp để bạn đánh giá lại phương pháp học tập và tìm kiếm các cách tiếp cận mới hiệu quả hơn.");
                }
            }

            return (improvementSb.ToString().Trim(), comparisonSb.ToString().Trim());
        }

        private static string ClassifyMarkLevel(double mark) => mark switch
        {
            >= 8.0 => "Xuất sắc",
            >= 7.0 => "Khá",
            >= 6.0 => "Trung bình",
            _ => "Yếu"
        };

        private static string GetMarkAssessment(double mark) => mark switch
        {
            >= 8.0 => "Bạn đã nắm vững kiến thức rất tốt, có thể tiếp tục phát triển ở mức nâng cao.",
            >= 7.0 => "Bạn đã nắm vững kiến thức ở mức khá, cần củng cố thêm để đạt kết quả tốt hơn.",
            >= 6.0 => "Bạn đã nắm vững kiến thức ở mức trung bình, cần cải thiện nhiều hơn.",
            _ => "Bạn cần củng cố kiến thức nền tảng ngay lập tức để tránh hổng kiến thức và nguy cơ rớt môn."
        };
        #endregion

        private static readonly JsonSerializerOptions AiResponseJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        /// <summary>
        /// Method chat use to call openAI
        /// </summary>
        /// <param name="systemPrompt"></param>
        /// <param name="userPrompt"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string?> CompleteJsonChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            try
            {
                ChatCompletion completion = await chat.CompleteChatAsync(
                    new ChatMessage[]
                    {
                        new SystemChatMessage(systemPrompt),
                        new UserChatMessage(userPrompt)
                    },
                    new ChatCompletionOptions
                    {
                        Temperature = 0.25f,
                        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                    },
                    ct);

                var content = (completion.Content?.Count > 0)
                    ? string.Concat(completion.Content.Select(c => c.Text))
                    : null;

                return string.IsNullOrWhiteSpace(content)
                    ? null
                    : AiQuizEvaluatorCommon.StripCodeFence(content);
            }
            catch
            {
                return null;
            }
        }

        private static List<AbilityMark>? EnsureAbilityCoverage(List<AbilityMark>? abilityMarks)
        {
            // Nếu abilityMarks null hoặc rỗng, trả về null để AI biết chưa có dữ liệu
            if (abilityMarks == null || abilityMarks.Count == 0)
            {
                return null;
            }

            // Normalize các ability marks có sẵn
            var normalized = abilityMarks.Select(a => new AbilityMark
            {
                Name = string.IsNullOrWhiteSpace(a.Name) ? "Khả năng chưa đặt tên" : a.Name,
                Mark = a.Mark
            }).ToList();

            // Không tự động thêm các ability thiếu với mark = 0
            // Để AI tự xử lý khi không có đủ dữ liệu
            return normalized;
        }

        private async Task<List<SubjectCur>> LoadCurriculumSubjectsAsync(AiRecommendImprovementRequest req, CancellationToken ct)
        {
            var fallback = NormalizeCurriculumSubjects(BuildCurriculumFallbackFromRequest(req));

            // ✅ UPDATED: Handle multiple majors - use first major or empty if none
            var majorCode = req.Majors != null && req.Majors.Count > 0
                ? NormalizeSubjectCode(req.Majors[0].MajorCode)
                : string.Empty;

            var requestedSubjectCodes = (req.SubjectMarks)
                .Select(mark => NormalizeSubjectCode(mark.SubjectCode))
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(majorCode) && requestedSubjectCodes.Count == 0)
            {
                return fallback;
            }

            var eventRequest = new SubjectInfoEvent
            {
                MajorCode = majorCode,
                SubjectCodes = requestedSubjectCodes
            };

            try
            {
                var response = await subjectInfoClient.GetResponse<SubjectInfoEventResponse>(eventRequest, ct);

                var message = response.Message;
                if (message.Success && message.Response is { Count: > 0 } items)
                {
                    var mappedSubjects = NormalizeCurriculumSubjects(
                        items.Select(item => new SubjectCur
                        {
                            SubjectCode = item.SubjectCode,
                            SubjectName = item.SubjectName,
                            Index = item.SemesterIndex.HasValue && item.SemesterIndex.Value > 0
                                ? item.SemesterIndex.Value
                                : item.SubjectIndex,
                            SubjectPrerequisiteCode = item.PrereqSubjectCodes
                        }));

                    if (fallback.Count == 0)
                    {
                        return mappedSubjects;
                    }

                    return mappedSubjects
                        .Concat(fallback)
                        .GroupBy(subject => subject.SubjectCode, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToList();
                }
            }
            catch
            {
                // Fall back to legacy payload if remote lookup fails
            }

            return fallback;
        }

        private static IEnumerable<SubjectCur> BuildCurriculumFallbackFromRequest(AiRecommendImprovementRequest req)
        {
            if (req.SubjectMarks is not { Count: > 0 })
            {
                return Array.Empty<SubjectCur>();
            }

            return req.SubjectMarks
                .Where(mark => !string.IsNullOrWhiteSpace(mark.SubjectCode))
                .Select(mark => new SubjectCur
                {
                    SubjectCode = mark.SubjectCode,
                    SubjectName = string.IsNullOrWhiteSpace(mark.SubjectName)
                        ? mark.SubjectCode
                        : mark.SubjectName,
                    Index = 0,
                    SubjectPrerequisiteCode = []
                })
                .ToList();
        }

        private static List<SubjectMark> EnsureSubjectCoverage(List<SubjectMark>? subjectMarks, List<SubjectCur> curriculumSubjects)
        {
            var normalized = subjectMarks?.Select(s => new SubjectMark
            {
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                Mark = s.Mark
            }).ToList() ?? new List<SubjectMark>();

            if (normalized.Count == 0 && curriculumSubjects.Count > 0)
            {
                normalized.AddRange(curriculumSubjects
                    .OrderBy(subject => subject.Index)
                    .Take(4)
                    .Select(subject => new SubjectMark
                    {
                        SubjectCode = subject.SubjectCode,
                        SubjectName = subject.SubjectName,
                        Mark = 0
                    }));
            }

            if (normalized.Count == 0)
            {
                normalized.Add(new SubjectMark
                {
                    SubjectCode = "-",
                    SubjectName = "Chưa có dữ liệu",
                    Mark = 0
                });
            }

            return normalized;
        }

        private static List<AbilityAnalysis>? TryParseAbilityAnalyses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<AbilityAnalysisEnvelope>(json, AiResponseJsonOptions);
                if (parsed?.AbilityAnalyses is { Count: > 0 } items)
                {
                    return items
                        .Where(item => !string.IsNullOrWhiteSpace(item.Name) &&
                                       !string.IsNullOrWhiteSpace(item.AnalysisMarkdown))
                        .ToList();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static List<SubjectAnalysis>? TryParseSubjectAnalyses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<SubjectAnalysisEnvelope>(json, AiResponseJsonOptions);
                if (parsed?.SubjectAnalyses is { Count: > 0 } items)
                {
                    return items
                        .Where(item => !string.IsNullOrWhiteSpace(item.SubjectName) &&
                                       !string.IsNullOrWhiteSpace(item.AnalysisMarkdown))
                        .ToList();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static List<SubjectWithoutMarkAnalysis>? TryParseWithoutMarkAnalyses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<DependencyAnalysisEnvelope>(json, AiResponseJsonOptions);
                if (parsed?.DependencyAnalyses is { Count: > 0 } items)
                {
                    return items
                        .Where(item => !string.IsNullOrWhiteSpace(item.SubjectCode) &&
                                       !string.IsNullOrWhiteSpace(item.AnalysisMarkdown))
                        .ToList();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static PersonaSummaryState? TryParsePersonaSummary(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<PersonaEnvelope>(json, AiResponseJsonOptions);
                return parsed == null
                    ? null
                    : new PersonaSummaryState(
                        parsed.SummaryFeedback ?? string.Empty,
                        parsed.HabitAndInterestAnalysis ?? string.Empty,
                        parsed.Personality ?? string.Empty,
                        parsed.LearningAbility ?? string.Empty);
            }
            catch
            {
                return null;
            }
        }

        private static List<AbilityAnalysis> BuildAbilityFallback(
            IReadOnlyCollection<AbilityMark> abilities,
            string careerGoal)
        {
            // Nếu không có ability marks (sinh viên kỳ 5+), không cần đánh giá năng lực cơ bản
            if (abilities == null || abilities.Count == 0)
            {
                return new List<AbilityAnalysis>
                {
                    new AbilityAnalysis
                    {
                        Name = "Phân tích dựa trên kết quả học tập",
                        AnalysisMarkdown = @"## Phân tích năng lực từ bảng điểm

                        ### Ghi chú
                        - Bạn đã có bảng điểm môn học đầy đủ, không cần đánh giá năng lực cơ bản riêng biệt.
                        - Hệ thống sẽ phân tích năng lực của bạn dựa trên **kết quả học tập thực tế** từ các môn đã hoàn thành.

                        ### Phân tích năng lực
                        - **Lập trình hướng đối tượng**: Được đánh giá qua các môn PRF192, PRO192, OOP.
                        - **Lập trình Web**: Được đánh giá qua các môn HTML/CSS, JavaScript, Web Development.
                        - **Cấu trúc dữ liệu & Giải thuật**: Được đánh giá qua các môn DSA, Algorithm, Data Structures.
                        - **Cơ sở dữ liệu**: Được đánh giá qua các môn DBI, Database Design, SQL.

                        ### Khuyến nghị
                        - Xem phần **Phân tích môn học** bên dưới để biết chi tiết về điểm mạnh/yếu của từng môn.
                        - Tập trung vào các môn có điểm dưới 7.0 để củng cố kiến thức nền tảng."
                    }
                };
            }

            return abilities.Select(ability =>
            {
                var sb = new StringBuilder();
                sb.AppendLine($"## {ability.Name}");
                sb.AppendLine("### Hiện trạng");
                sb.AppendLine($"- Điểm hiện tại: **{ability.Mark}/10** · Mức: {ClassifyScore(ability.Mark)}.");
                sb.AppendLine("- Chưa có báo cáo AI chi tiết nên tạm dùng đánh giá tổng quát.");
                sb.AppendLine("### Kiến thức trọng tâm");
                if (ability.Mark >= 8.0)
                {
                    sb.AppendLine($"- Đào sâu các mẫu thiết kế nâng cao của {ability.Name}, thực hành refactor.");
                    sb.AppendLine("- Tập trung tối ưu hoá hiệu năng và viết tài liệu kỹ thuật song song.");
                }
                else if (ability.Mark >= 6.0)
                {
                    sb.AppendLine($"- Ôn lại cấu trúc dữ liệu/cú pháp chính của {ability.Name} bằng flashcard + mindmap.");
                    sb.AppendLine("- Làm lại bộ bài mẫu chuẩn (ít nhất 5 bài) để củng cố phản xạ.");
                }
                else
                {
                    sb.AppendLine($"- Bắt đầu từ tài liệu nền tảng của {ability.Name}: biến, hàm, cấu trúc điều khiển.");
                    sb.AppendLine("- Nhờ mentor kiểm tra lại từng bước để tránh sai sót cơ bản.");
                }
                sb.AppendLine("### Lộ trình 2–4 tuần");
                if (ability.Mark >= 8.0)
                {
                    sb.AppendLine("- Tuần 1-2: 2 project mini/tuần bám sát case thực tế, viết post-mortem sau mỗi project.");
                    sb.AppendLine("- Tuần 3-4: Mentor review code + luyện thử phỏng vấn hệ thống trong 2 buổi.");
                }
                else if (ability.Mark >= 6.0)
                {
                    sb.AppendLine("- Tuần 1-2: 3 buổi/tuần ôn lý thuyết + 2 buổi luyện bài có chấm điểm.");
                    sb.AppendLine("- Tuần 3-4: Ghép bài tập áp dụng vào mini project, tổng ít nhất 8 bài.");
                }
                else
                {
                    sb.AppendLine("- Tuần 1-2: 4 buổi/tuần học lại giáo trình chuẩn, ghi chú lại từng concept khó.");
                    sb.AppendLine("- Tuần 3-4: Làm 6-8 bài cơ bản và nhờ mentor/bạn học phản hồi.");
                }
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    sb.AppendLine($"- Liên kết với mục tiêu {careerGoal}: mỗi tuần chọn 1 bài tập mô phỏng yêu cầu công việc thực tế.");
                }
                sb.AppendLine("- Viết nhật ký cải thiện sau mỗi tuần để đo tiến bộ và điều chỉnh.");

                return new AbilityAnalysis
                {
                    Name = ability.Name,
                    AnalysisMarkdown = sb.ToString().Trim()
                };
            }).ToList();
        }

        private static List<SubjectAnalysis> BuildSubjectFallback(
            IReadOnlyCollection<SubjectMark> subjects,
            IReadOnlyCollection<SubjectCur> curriculumSubjects,
            string careerGoal)
        {
            var source = subjects.Count > 0
                ? subjects
                : curriculumSubjects.Select(s => new SubjectMark
                {
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Mark = 0
                }).ToList();

            if (source.Count == 0)
            {
                var fallbackMarkdown = new StringBuilder();
                fallbackMarkdown.AppendLine("## Tổng quan môn học");
                fallbackMarkdown.AppendLine("### Tình hình");
                fallbackMarkdown.AppendLine("- Chưa có điểm môn học nào để phân tích chi tiết.");
                fallbackMarkdown.AppendLine("### Kiến thức trọng tâm");
                fallbackMarkdown.AppendLine("- Ôn lại giáo trình cơ sở (PRF, OOP, cấu trúc dữ liệu) để tạo dữ liệu đầu vào.");
                fallbackMarkdown.AppendLine("### Lộ trình 2–4 tuần");
                fallbackMarkdown.AppendLine("- Bổ sung bảng điểm tối thiểu 3 môn và cập nhật sau mỗi bài kiểm tra.");
                fallbackMarkdown.AppendLine("- Thu thập tài liệu môn nền tảng và ghi chú các phần chưa chắc.");
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    fallbackMarkdown.AppendLine($"- Liệt kê 2 môn quan trọng nhất với mục tiêu {careerGoal} để ưu tiên nhập điểm.");
                }

                return
                [
                    new SubjectAnalysis
                    {
                        SubjectCode = "-",
                        SubjectName = "Chưa có dữ liệu",
                        AnalysisMarkdown = fallbackMarkdown.ToString().Trim()
                    }
                ];
            }

            var normalizedCurriculum = NormalizeCurriculumSubjects(curriculumSubjects);
            var dependencyMap = BuildSubjectDependencyMap(normalizedCurriculum);
            var curriculumLookup = normalizedCurriculum
                .ToDictionary(x => x.SubjectCode, x => x, StringComparer.OrdinalIgnoreCase);
            var markLookup = source
                .Where(x => !string.IsNullOrWhiteSpace(x.SubjectCode))
                .GroupBy(x => NormalizeSubjectCode(x.SubjectCode))
                .ToDictionary(g => g.Key, g => g.First().Mark!.Value, StringComparer.OrdinalIgnoreCase);

            return source.Select(subject =>
            {
                var subjectCodeNormalized = NormalizeSubjectCode(subject.SubjectCode);
                curriculumLookup.TryGetValue(subjectCodeNormalized, out var subjectInfo);
                var subjectSemesterLabel = FormatSemesterLabel(subjectInfo?.Index);
                var subjectDisplayCode = string.IsNullOrWhiteSpace(subject.SubjectCode)
                    ? subjectCodeNormalized
                    : subject.SubjectCode.Trim();
                var subjectDisplayName = string.IsNullOrWhiteSpace(subject.SubjectName)
                    ? subjectDisplayCode
                    : subject.SubjectName.Trim();
                var subjectScore = subject.Mark ?? 0;

                var sb = new StringBuilder();
                sb.AppendLine($"## {subjectDisplayName} ({subjectDisplayCode})");
                sb.AppendLine("### Tình hình");
                sb.AppendLine($"- Điểm hiện tại: **{subjectScore}/10** · Mức: {ClassifyScore(subjectScore)}.");
                if (subjectSemesterLabel is not null)
                {
                    sb.AppendLine($"- Thuộc {subjectSemesterLabel} trong chương trình; cần giữ tiến độ để tránh dồn môn.");
                }
                if (subjectScore >= 8.0)
                {
                    sb.AppendLine("- Năng lực khá ổn, có thể thử thách bằng đề mở rộng hoặc dự án nhỏ.");
                }
                else if (subjectScore >= 6.5)
                {
                    sb.AppendLine("- Nên củng cố lại các chủ đề bị sai và tăng tần suất luyện đề.");
                }
                else
                {
                    sb.AppendLine("- Cần ôn lại lý thuyết nền và làm lại bài tập cơ bản để tránh hổng kiến thức.");
                }

                sb.AppendLine("### Kiến thức trọng tâm");
                if (subjectScore >= 8.0)
                {
                    sb.AppendLine($"- Đào sâu các chủ đề nâng cao của {subjectDisplayName} (tối ưu, mô hình hoá, kiến trúc).");
                    sb.AppendLine("- Viết lại insight sau mỗi bài để chuẩn bị cho môn liên quan.");
                }
                else if (subjectScore >= 6.5)
                {
                    sb.AppendLine($"- Rà lại 2 chương trọng yếu của {subjectDisplayName}, ghi chú công thức/thuật toán chính.");
                    sb.AppendLine("- Hoàn thành 3-4 bài tập chuẩn để kiểm tra hiểu bài.");
                }
                else
                {
                    sb.AppendLine("- Ôn lại lý thuyết nền từ giáo trình/video chính thức và luyện ví dụ đơn giản.");
                    sb.AppendLine("- Nhờ mentor/bạn học giải thích các phần còn mơ hồ trước khi luyện bài mới.");
                }

                var warnings = new List<string>();

                var prerequisiteBullets = BuildPrerequisiteBullets(subjectInfo, subjectDisplayName, curriculumLookup, markLookup, warnings);
                var dependencyWarnings = BuildDependencyWarnings(subjectCodeNormalized, subjectDisplayName, subjectScore, dependencyMap);
                warnings.AddRange(dependencyWarnings);

                sb.AppendLine("### Môn tiền đề quan trọng");
                AppendBullets(sb, prerequisiteBullets);

                if (warnings.Count > 0)
                {
                    sb.AppendLine("### Cảnh báo");
                    AppendBullets(sb, warnings.Distinct().ToList());
                }

                sb.AppendLine("### Lộ trình 2–4 tuần");
                if (subjectScore >= 8.0)
                {
                    sb.AppendLine("- Tuần 1-2: 2 buổi ôn lý thuyết nâng cao + 2 buổi luyện đề theo dự án.");
                    sb.AppendLine("- Tuần 3-4: Viết tóm tắt nội dung và chia sẻ/mentor review.");
                }
                else if (subjectScore >= 6.5)
                {
                    sb.AppendLine("- Tuần 1-2: 3 buổi củng cố lý thuyết + 2 buổi làm bài chuẩn hóa.");
                    sb.AppendLine("- Tuần 3-4: Làm thêm 4-5 bài nâng dần độ khó và tự chấm lại.");
                }
                else
                {
                    sb.AppendLine("- Tuần 1-2: 4 buổi/tuần học lại lý thuyết nền, ghi chú từng ví dụ.");
                    sb.AppendLine("- Tuần 3-4: 6 bài cơ bản + 2 buổi giải đáp với mentor/bạn học.");
                }
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    sb.AppendLine($"- Chọn 1 chủ đề trong {subjectDisplayName} liên quan tới {careerGoal} để làm mini note.");
                }
                sb.AppendLine("- Đánh giá lại bằng quiz hoặc flashcard sau mỗi 2 tuần.");

                return new SubjectAnalysis
                {
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    AnalysisMarkdown = sb.ToString().Trim()
                };
            }).ToList();
        }

        private static IEnumerable<List<SubjectMark>> ChunkSubjects(List<SubjectMark> subjects, int batchSize)
        {
            if (subjects.Count == 0 || batchSize <= 0)
            {
                yield break;
            }

            for (var i = 0; i < subjects.Count; i += batchSize)
            {
                yield return subjects.Skip(i).Take(batchSize).ToList();
            }
        }

        private static Dictionary<string, List<SubjectDependency>> BuildSubjectDependencyMap(IEnumerable<SubjectCur> subjects)
        {
            var map = new Dictionary<string, List<SubjectDependency>>(StringComparer.OrdinalIgnoreCase);
            foreach (var subject in subjects)
            {
                var subjectCode = NormalizeSubjectCode(subject.SubjectCode);
                if (string.IsNullOrWhiteSpace(subjectCode))
                {
                    continue;
                }

                foreach (var prerequisiteRaw in subject.SubjectPrerequisiteCode)
                {
                    var prerequisite = NormalizeSubjectCode(prerequisiteRaw);
                    if (string.IsNullOrWhiteSpace(prerequisite)) continue;

                    if (!map.TryGetValue(prerequisite, out var list))
                    {
                        list = new List<SubjectDependency>();
                        map[prerequisite] = list;
                    }

                    var dependentName = string.IsNullOrWhiteSpace(subject.SubjectName)
                        ? subjectCode
                        : subject.SubjectName.Trim();
                    var dependent = new SubjectDependency(
                        subjectCode,
                        dependentName,
                        subject.Index > 0 ? subject.Index : (int?)null);

                    if (!list.Any(item => item.SubjectCode.Equals(subjectCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(dependent);
                    }
                }
            }

            return map;
        }

        private static string? FormatSemesterLabel(int? semesterIndex) =>
            semesterIndex.HasValue && semesterIndex.Value > 0
                ? $"Kỳ {semesterIndex.Value}"
                : null;

        private static string NormalizeSubjectCode(string? code) =>
            string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

        private static List<SubjectCur> NormalizeCurriculumSubjects(IEnumerable<SubjectCur> subjects)
        {
            if (subjects is null)
            {
                return new List<SubjectCur>();
            }

            return subjects
                .Where(subject => subject is not null)
                .Select(subject =>
                {
                    var normalizedCode = NormalizeSubjectCode(subject.SubjectCode);
                    if (string.IsNullOrWhiteSpace(normalizedCode))
                    {
                        return null;
                    }

                    var normalizedPrereqs = (subject.SubjectPrerequisiteCode)
                        .Select(NormalizeSubjectCode)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new SubjectCur
                    {
                        SubjectCode = normalizedCode,
                        SubjectName = string.IsNullOrWhiteSpace(subject.SubjectName)
                            ? normalizedCode
                            : subject.SubjectName.Trim(),
                        Index = subject.Index,
                        SubjectPrerequisiteCode = normalizedPrereqs
                    };
                })
                .Where(subject => subject is not null)
                .Select(subject => subject!)
                .GroupBy(subject => subject.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static List<string> BuildPrerequisiteBullets(
            SubjectCur? subjectInfo,
            string subjectDisplayName,
            Dictionary<string, SubjectCur> curriculumLookup,
            Dictionary<string, double> markLookup,
            List<string> warningCollector)
        {
            var bullets = new List<string>();

            if (subjectInfo?.SubjectPrerequisiteCode == null || subjectInfo.SubjectPrerequisiteCode.Count == 0)
            {
                bullets.Add("Không có môn tiền đề.");
                return bullets;
            }

            foreach (var prereqCodeRaw in subjectInfo.SubjectPrerequisiteCode)
            {
                var prereqCode = NormalizeSubjectCode(prereqCodeRaw);
                if (string.IsNullOrWhiteSpace(prereqCode)) continue;

                curriculumLookup.TryGetValue(prereqCode, out var prereqInfo);
                var prereqDisplayCode = prereqInfo?.SubjectCode ?? prereqCode;
                var prereqDisplayName = string.IsNullOrWhiteSpace(prereqInfo?.SubjectName)
                    ? prereqDisplayCode
                    : prereqInfo.SubjectName.Trim();
                var prereqSemesterLabel = FormatSemesterLabel(prereqInfo?.Index);
                var prereqDisplay = prereqSemesterLabel is null
                    ? $"{prereqDisplayName} ({prereqDisplayCode})"
                    : $"{prereqDisplayName} ({prereqDisplayCode}) – {prereqSemesterLabel}";

                if (markLookup.TryGetValue(prereqCode, out var prereqMark))
                {
                    var status = prereqMark >= 7.0
                        ? $"điểm {prereqMark}/10 – đã đạt chuẩn, duy trì nhịp ôn để hỗ trợ {subjectDisplayName}"
                        : $"điểm {prereqMark}/10 – đang dưới chuẩn, ưu tiên củng cố trước khi học sâu {subjectDisplayName}";
                    bullets.Add($"{prereqDisplay}: {status}");

                    if (prereqMark < 7.0)
                    {
                        warningCollector.Add($"⚠️ {prereqDisplay}: điểm {prereqMark}/10 đang dưới chuẩn nhưng là tiền đề của {subjectDisplayName}. Cần củng cố sớm.");
                    }
                }
                else
                {
                    bullets.Add($"{prereqDisplay}: Chưa có điểm – cần hoàn thành trước khi học sâu {subjectDisplayName}.");
                    warningCollector.Add($"⚠️ {prereqDisplay}: chưa có điểm nhưng là tiền đề của {subjectDisplayName}; bổ sung dữ liệu để tránh rủi ro.");
                }
            }

            if (bullets.Count == 0)
            {
                bullets.Add("Không có môn tiền đề.");
            }

            return bullets;
        }

        private static List<string> BuildDependencyWarnings(
            string normalizedSubjectCode,
            string subjectDisplayName,
            double subjectMark,
            Dictionary<string, List<SubjectDependency>> dependencyMap)
        {
            var warnings = new List<string>();

            if (dependencyMap.TryGetValue(normalizedSubjectCode, out var dependents) &&
                dependents.Count > 0 &&
                subjectMark < 7.0)
            {
                foreach (var dependent in dependents)
                {
                    var dependentLabel = dependent.SemesterIndex.HasValue
                        ? $"{dependent.SubjectName} (kỳ {dependent.SemesterIndex})"
                        : dependent.SubjectName;
                    warnings.Add($"⚠️ {subjectDisplayName} là điều kiện đầu vào cho {dependentLabel}. Điểm hiện tại {subjectMark}/10 có thể khiến môn sau khó bắt nhịp nếu không củng cố.");
                }
            }

            return warnings;
        }

        private static void AppendBullets(StringBuilder sb, IReadOnlyList<string> bullets)
        {
            if (bullets == null || bullets.Count == 0)
            {
                sb.AppendLine("- Không có.");
                return;
            }

            foreach (var line in bullets)
            {
                sb.AppendLine($"- {line}");
            }
        }

        private static string UpsertSection(string markdown, string heading, IReadOnlyList<string> bullets)
        {
            var lines = (bullets is { Count: > 0 })
                ? bullets
                : new List<string> { "Không có." };

            var content = new StringBuilder();
            foreach (var line in lines)
            {
                content.AppendLine($"- {line}");
            }

            var sectionBody = $"{heading}\n{content}".TrimEnd();
            var pattern = $"{Regex.Escape(heading)}\\n(?:(?!\\n### ).)*";

            if (Regex.IsMatch(markdown, pattern, RegexOptions.Singleline))
            {
                return Regex.Replace(markdown, pattern, sectionBody, RegexOptions.Singleline);
            }

            return markdown.TrimEnd() + "\n\n" + sectionBody;
        }

        private static void EnrichSubjectAnalysesWithAcademicData(
            IEnumerable<SubjectAnalysis> analyses,
            List<SubjectCur> curriculumSubjects,
            List<SubjectMark> subjectMarks)
        {
            if (analyses is null) return;

            var normalizedCurriculum = NormalizeCurriculumSubjects(curriculumSubjects ?? new List<SubjectCur>());
            if (normalizedCurriculum.Count == 0) return;

            var curriculumLookup = normalizedCurriculum.ToDictionary(x => x.SubjectCode, x => x, StringComparer.OrdinalIgnoreCase);
            var dependencyMap = BuildSubjectDependencyMap(normalizedCurriculum);
            var markLookup = subjectMarks
                .Where(x => !string.IsNullOrWhiteSpace(x.SubjectCode) && x.Mark.HasValue)
                .GroupBy(x => NormalizeSubjectCode(x.SubjectCode))
                .ToDictionary(g => g.Key, g => g.First().Mark!.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var analysis in analyses)
            {
                if (analysis is null) continue;

                var normalizedCode = NormalizeSubjectCode(analysis.SubjectCode);
                if (string.IsNullOrWhiteSpace(normalizedCode)) continue;

                curriculumLookup.TryGetValue(normalizedCode, out var subjectInfo);
                var subjectDisplayName = string.IsNullOrWhiteSpace(analysis.SubjectName)
                    ? normalizedCode
                    : analysis.SubjectName.Trim();
                var subjectMark = markLookup.TryGetValue(normalizedCode, out var mark) ? mark : 0;

                var warnings = new List<string>();
                var prereqBullets = BuildPrerequisiteBullets(subjectInfo, subjectDisplayName, curriculumLookup, markLookup, warnings);
                var dependencyWarnings = BuildDependencyWarnings(normalizedCode, subjectDisplayName, subjectMark, dependencyMap);
                warnings.AddRange(dependencyWarnings);

                analysis.AnalysisMarkdown = UpsertSection(analysis.AnalysisMarkdown, "### Môn tiền đề quan trọng", prereqBullets);
                analysis.AnalysisMarkdown = UpsertSection(analysis.AnalysisMarkdown, "### Cảnh báo", warnings.Distinct().ToList());
            }
        }

        private static List<SubjectWithoutMarkAnalysis> BuildWithoutMarkFallback(
            IEnumerable<SubjectMark> missingSubjects,
            List<SubjectCur> curriculumSubjects,
            string careerGoal)
        {
            var result = new List<SubjectWithoutMarkAnalysis>();
            var normalizedCurriculum = NormalizeCurriculumSubjects(curriculumSubjects ?? new List<SubjectCur>());
            if (normalizedCurriculum.Count == 0) return result;

            var curriculumLookup = normalizedCurriculum.ToDictionary(x => x.SubjectCode, x => x, StringComparer.OrdinalIgnoreCase);
            var dependencyMap = BuildSubjectDependencyMap(normalizedCurriculum);

            foreach (var subject in missingSubjects ?? Enumerable.Empty<SubjectMark>())
            {
                var normalizedCode = NormalizeSubjectCode(subject.SubjectCode);
                if (string.IsNullOrWhiteSpace(normalizedCode)) continue;

                var displayName = string.IsNullOrWhiteSpace(subject.SubjectName)
                    ? normalizedCode
                    : subject.SubjectName.Trim();

                curriculumLookup.TryGetValue(normalizedCode, out var subjectInfo);

                var dependentBullets = new List<string>();
                if (dependencyMap.TryGetValue(normalizedCode, out var dependents) && dependents.Count > 0)
                {
                    foreach (var dependent in dependents)
                    {
                        var semesterLabel = FormatSemesterLabel(dependent.SemesterIndex);
                        var suffix = semesterLabel is null ? "trong các kỳ sau" : $"ở {semesterLabel}";
                        dependentBullets.Add($"{dependent.SubjectName} ({dependent.SubjectCode}) {suffix} – cần nền tảng {displayName} để theo kịp tiến độ.");
                    }
                }
                else
                {
                    dependentBullets.Add("Không có môn phụ thuộc trực tiếp.");
                }

                var actionBullets = new List<string>
                {
                    "Hoàn thành ít nhất 1 bài đánh giá/quiz để cập nhật điểm và phát hiện khoảng trống kiến thức.",
                    "Ôn lại slide/bài giảng trọng tâm và ghi chú 3-4 câu hỏi tự kiểm tra trước khi bước vào môn phụ thuộc."
                };

                if (subjectInfo?.SubjectPrerequisiteCode is { Count: > 0 })
                {
                    var prereqNames = subjectInfo.SubjectPrerequisiteCode
                        .Select(NormalizeSubjectCode)
                        .Where(code => !string.IsNullOrWhiteSpace(code))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(code =>
                        {
                            curriculumLookup.TryGetValue(code, out var prereq);
                            return string.IsNullOrWhiteSpace(prereq?.SubjectName) ? code : prereq.SubjectName.Trim();
                        })
                        .ToList();

                    if (prereqNames.Count > 0)
                    {
                        actionBullets.Add($"Rà soát nhanh các môn nền tảng: {string.Join(", ", prereqNames)} để đảm bảo nền vững trước khi cập nhật điểm.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    actionBullets.Add($"Liên hệ mục tiêu {careerGoal}: mô phỏng 1 mini-task thực tế sau khi cập nhật điểm để khóa kiến thức.");
                }

                var sb = new StringBuilder();
                sb.AppendLine($"## {displayName} ({normalizedCode})");
                sb.AppendLine("### Vì sao nên chuẩn bị sớm");
                sb.AppendLine("- Môn này chưa được học/đánh giá; chuẩn bị trước giúp tránh hụt hơi khi bước vào kỳ chính.");
                sb.AppendLine("### Môn phụ thuộc dễ bị ảnh hưởng");
                AppendBullets(sb, dependentBullets);
                sb.AppendLine("### Hành động cần làm");
                AppendBullets(sb, actionBullets);

                result.Add(new SubjectWithoutMarkAnalysis
                {
                    SubjectCode = normalizedCode,
                    SubjectName = displayName,
                    AnalysisMarkdown = sb.ToString().Trim()
                });
            }

            return result;
        }

        private static PersonaSummaryState BuildPersonaFallback(
            IReadOnlyCollection<SubjectMark> subjectMarks,
            IReadOnlyCollection<AbilityMark> abilityMarks,
            QuizSurvey quizSurvey,
            string careerGoal)
        {
            var scoredSubjects = subjectMarks.Where(s => s.Mark.HasValue).ToList();
            var avgSubject = scoredSubjects.Count > 0 ? scoredSubjects.Average(s => s.Mark!.Value) : 0;
            var avgAbility = (abilityMarks != null && abilityMarks.Count > 0) ? abilityMarks.Average(a => a.Mark) : 0;
            var strongSubjects = scoredSubjects.Where(s => s.Mark >= 8.0).Select(s => s.SubjectName).ToList();
            var weakSubjects = scoredSubjects.Where(s => s.Mark < 6.5).Select(s => s.SubjectName).ToList();
            var strongAbilities = (abilityMarks != null) ? abilityMarks.Where(a => a.Mark >= 8.0).Select(a => a.Name).ToList() : new List<string>();
            var weakAbilities = (abilityMarks != null) ? abilityMarks.Where(a => a.Mark < 6.5).Select(a => a.Name).ToList() : new List<string>();

            var interests = quizSurvey?.QuizInterests ?? new List<QuizInterest>();
            var habits = quizSurvey?.QuizHabits ?? new List<QuizHabit>();

            var summaryBuilder = new StringBuilder();
            summaryBuilder.AppendLine("## Tổng quan");
            if (scoredSubjects.Count == 0)
            {
                // Không có điểm môn học, đánh giá dựa trên abilityMarks và quizSurvey
                if (abilityMarks != null && abilityMarks.Count > 0)
                {
                    summaryBuilder.AppendLine($"- Đánh giá dựa trên năng lực hiện tại: điểm trung bình {avgAbility:F1}/10.");
                    if (strongAbilities.Count > 0)
                    {
                        summaryBuilder.AppendLine($"- Thế mạnh nổi bật: {string.Join(", ", strongAbilities.Take(2))}.");
                    }
                    if (weakAbilities.Count > 0)
                    {
                        summaryBuilder.AppendLine($"- Cần củng cố: {string.Join(", ", weakAbilities.Take(2))}.");
                    }
                }
                else
                {
                    summaryBuilder.AppendLine("- Chưa có dữ liệu điểm môn học hoặc năng lực để đánh giá tổng quan.");
                }

                if (habits.Count > 0 || interests.Count > 0)
                {
                    if (habits.Count > 0)
                    {
                        summaryBuilder.AppendLine($"- Thói quen học tập: {habits[0].Answer}.");
                    }
                    if (interests.Count > 0)
                    {
                        summaryBuilder.AppendLine($"- Sở thích học tập: {interests[0].Answer}.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    summaryBuilder.AppendLine($"- Mục tiêu nghề nghiệp: {careerGoal}. Xây dựng lộ trình học tập phù hợp với mục tiêu này.");
                }
            }
            else
            {
                summaryBuilder.AppendLine($"- Điểm trung bình các môn đang quanh mức {avgSubject:F1}/10.");
                if (strongSubjects.Count > 0)
                {
                    summaryBuilder.AppendLine($"- Điểm mạnh: {string.Join(", ", strongSubjects.Take(2))}.");
                }
                if (weakSubjects.Count > 0)
                {
                    summaryBuilder.AppendLine($"- Ưu tiên củng cố: {string.Join(", ", weakSubjects.Take(2))}.");
                }
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    summaryBuilder.AppendLine($"- Liên hệ trực tiếp với mục tiêu {careerGoal} để chọn môn ưu tiên và đặt KPI 2–4 tuần.");
                }
            }
            summaryBuilder.AppendLine("- Hành động: đặt checklist môn ưu tiên và cập nhật tiến độ mỗi tuần.");

            var habitBuilder = new StringBuilder();
            habitBuilder.AppendLine("## Thói quen & Sở thích");
            if (habits.Count == 0 && interests.Count == 0)
            {
                habitBuilder.AppendLine("- Chưa có phản hồi khảo sát nên khó phân tích thói quen. Hoàn thành bảng hỏi để AI cá nhân hoá lộ trình.");
            }
            else
            {
                if (habits.Count > 0)
                {
                    habitBuilder.AppendLine($"- Thói quen học nổi bật: \"{habits.First().Answer}\".");
                }
                if (interests.Count > 0)
                {
                    habitBuilder.AppendLine($"- Sở thích học tập: \"{interests[0].Answer}\".");
                }
                habitBuilder.AppendLine("- Khai thác các yếu tố này để giữ động lực ổn định mỗi tuần.");
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    habitBuilder.AppendLine($"- Liên hệ thói quen/sở thích với mục tiêu {careerGoal} bằng các bài tập mô phỏng môi trường làm việc mong muốn.");
                }
            }
            habitBuilder.AppendLine("- Hành động: cố định 3 khung giờ học/tuần và dùng sở thích để thiết kế bài tập dài hơi.");

            var personalityBuilder = new StringBuilder();
            personalityBuilder.AppendLine("## Phong cách học tập");

            // Xác định phong cách dựa trên dữ liệu có sẵn
            double baseScore = scoredSubjects.Count > 0 ? avgSubject : (abilityMarks != null && abilityMarks.Count > 0 ? avgAbility : 0);

            if (scoredSubjects.Count == 0 && (abilityMarks == null || abilityMarks.Count == 0))
            {
                // Không có dữ liệu điểm, dựa vào quizSurvey
                if (habits.Count > 0)
                {
                    personalityBuilder.AppendLine($"- Phong cách học tập được phản ánh qua thói quen: \"{habits[0].Answer}\".");
                }
                else
                {
                    personalityBuilder.AppendLine("- Chưa có đủ dữ liệu để đánh giá phong cách học tập. Hoàn thành khảo sát để có đánh giá chính xác hơn.");
                }
            }
            else
            {
                personalityBuilder.Append("- Người học cho thấy phong cách ");
                personalityBuilder.Append(baseScore >= 7.5 ? "kỷ luật và thiên về hệ thống" : "linh hoạt nhưng cần thêm cấu trúc");
                if (habits.Count > 0)
                {
                    personalityBuilder.Append($", phản ánh trong chia sẻ \"{habits[0].Answer}\"");
                }
                personalityBuilder.AppendLine(". Duy trì phản hồi sau mỗi buổi học để tự điều chỉnh.");
            }
            personalityBuilder.AppendLine("- Hành động: sau mỗi tuần, tự đánh giá điểm tập trung và điều chỉnh phương pháp cho tuần kế tiếp.");

            var learningAbilityBuilder = new StringBuilder();
            learningAbilityBuilder.AppendLine("## Năng lực học & Lộ trình");
            if (abilityMarks == null || abilityMarks.Count == 0)
            {
                // Sinh viên kỳ 5+ đã có bảng điểm đầy đủ, phân tích dựa trên môn học
                learningAbilityBuilder.AppendLine("- Bạn đã có bảng điểm môn học đầy đủ, hệ thống phân tích năng lực dựa trên kết quả học tập thực tế.");
                if (scoredSubjects.Count > 0)
                {
                    learningAbilityBuilder.AppendLine($"- Điểm trung bình các môn: {avgSubject:F1}/10 - phản ánh năng lực tổng thể của bạn.");

                    var techSubjects = scoredSubjects.Where(s =>
                        s.SubjectCode.Contains("PRO") || s.SubjectCode.Contains("PRF") ||
                        s.SubjectCode.Contains("WEB") || s.SubjectCode.Contains("DSA") ||
                        s.SubjectCode.Contains("DBI")).ToList();

                    if (techSubjects.Count > 0)
                    {
                        var avgTech = techSubjects.Average(s => s.Mark!.Value);
                        learningAbilityBuilder.AppendLine($"- Năng lực kỹ thuật (các môn chuyên ngành): {avgTech:F1}/10.");
                    }
                }
                learningAbilityBuilder.AppendLine("- Khuyến nghị: Tập trung vào các môn chuyên sâu và dự án thực tế thay vì ôn lại kiến thức cơ bản.");
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    learningAbilityBuilder.AppendLine($"- Xây dựng portfolio và kinh nghiệm thực tế phù hợp với mục tiêu {careerGoal}.");
                }
            }
            else
            {
                learningAbilityBuilder.AppendLine($"- Điểm trung bình năng lực khoảng {avgAbility:F1}/10.");
                if (strongAbilities.Count > 0)
                {
                    learningAbilityBuilder.AppendLine($"- Thế mạnh: {string.Join(", ", strongAbilities.Take(2))}.");
                }
                if (weakAbilities.Count > 0)
                {
                    learningAbilityBuilder.AppendLine($"- Cần cải thiện: {string.Join(", ", weakAbilities.Take(2))}.");
                }
                learningAbilityBuilder.AppendLine("- Thiết lập chu kỳ ôn luyện 14 ngày và đo lại bằng quiz tự tạo.");
                if (!string.IsNullOrWhiteSpace(careerGoal))
                {
                    learningAbilityBuilder.AppendLine($"- Nhắc nhở tiêu chí của {careerGoal} trước mỗi phiên học.");
                }
            }
            learningAbilityBuilder.AppendLine("- Hành động: đặt mục tiêu 2–4 tuần với thang đo cụ thể (số bài, dự án nhỏ) và review định kỳ.");

            return new PersonaSummaryState(
                summaryBuilder.ToString(),
                habitBuilder.ToString(),
                personalityBuilder.ToString(),
                learningAbilityBuilder.ToString());
        }

        private static string ClassifyScore(double mark) => mark switch
        {
            >= 8.5 => "Giỏi",
            >= 7.0 => "Khá",
            >= 5.0 => "Cần củng cố",
            _ => "Nguy cơ"
        };

        private sealed class AbilityAnalysisEnvelope
        {
            public List<AbilityAnalysis>? AbilityAnalyses { get; set; } = [];
        }

        private sealed class SubjectAnalysisEnvelope
        {
            public List<SubjectAnalysis>? SubjectAnalyses { get; set; } = [];
        }

        private sealed class PersonaEnvelope
        {
            public string? SummaryFeedback { get; set; } = string.Empty;
            public string? HabitAndInterestAnalysis { get; set; } = string.Empty;
            public string? Personality { get; set; } = string.Empty;
            public string? LearningAbility { get; set; } = string.Empty;
        }

        private sealed class DependencyAnalysisEnvelope
        {
            public List<SubjectWithoutMarkAnalysis>? DependencyAnalyses { get; set; } = [];
        }

        private sealed record SubjectDependency(string SubjectCode, string SubjectName, int? SemesterIndex);

        private sealed record PersonaSummaryState(
            string summaryFeedback,
            string habitAndInterestAnalysis,
            string personality,
            string learningAbility);
    }
}

