using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Infrastructure.Implements
{
    public class AdvisorService : IAdvisorService
    {
        private readonly IMajorService _service;
        private readonly IVectorSearchService _search;
        private readonly ChatClient _chat;
        private readonly EmbeddingClient _embed;
        private const int HOURS_PER_WEEK = 5;
        public AdvisorService(IMajorService service, IVectorSearchService search, ChatClient chat, EmbeddingClient embed)
        {
            _service = service;
            _search = search;
            _chat = chat;
            _embed = embed;
        }

        public async Task<EvaluateResult> EvaluateAsync(AiEvaluateRequest req, CancellationToken ct)
        {
            try
            {
                int k = req.KRetrieval <= 0 ? 4 : req.KRetrieval;
                int threshold = Math.Clamp(req.ScoreThreshold, 0, 100);

                var kf = (req.KnownFrameworks ?? new()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                var kl = (req.KnownLanguages ?? new()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                // 1) Embedding cho truy vấn
                string queryText = $"Career goal: {req.CareerGoal}. Frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}. Languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}.";
                var embRes = await _embed.GenerateEmbeddingAsync(queryText, cancellationToken: ct);
                var embVal = embRes.Value;
                var qvec = embVal.ToFloats().ToArray();

                // 2) Lấy majors + search top-k bằng EF + pgvector
                var majors = await _service.LoadMajorsAsync(ct);
                var hits = await _search.SearchTopKAsync(qvec, k, ct);
                var hitCodes = hits.Select(h => h.MajorCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var targets = majors.Where(m => hitCodes.Contains(m.MajorCode)).ToList();
                if (targets.Count == 0) targets = majors.Take(k).ToList();

                // 3) Đánh giá từng major bằng ChatClient
                var evals = new List<MajorEvaluation>();
                foreach (var mj in targets)
                {
                    string sys = """
                    Bạn là cố vấn hướng nghiệp CNTT. Nhiệm vụ: ĐÁNH GIÁ MỨC ĐỘ PHÙ HỢP của MỘT ngành học (major) so với mục tiêu nghề nghiệp và stack đã biết.

                    Nguyên tắc CHẤM ĐIỂM (0–100, số nguyên):
                    - Trọng số: 70% bám sát mục tiêu nghề nghiệp (career_goal), 30% khớp công nghệ/ngôn ngữ (frameworks/languages).
                    - Phân loại theo mức độ gần domain:
                      A) TRỰC TIẾP (Direct Domain Match): tên/code/miêu tả major cho thấy cùng miền với career_goal (vd: “Game”, “Gaming”, “Trò chơi”, “Unity/Unreal game”, “Game programming”). 
                         → Khung điểm nền: 85–100.
                      B) LÂN CẬN/HỖ TRỢ (Adjacent/Supportive): backend, frontend, .NET/Java/React… hỗ trợ làm game (server, tools, pipeline) nhưng không phải domain chính.
                         → Khung điểm nền: 60–84.
                      C) XA MIỀN (Distant/Unrelated): không hỗ trợ rõ ràng cho career_goal.
                         → Khung điểm nền: 0–59.

                    RÀNG BUỘC XẾP HẠNG:
                    - Nếu tồn tại major TRỰC TIẾP với career_goal, thì major đó PHẢI có điểm cao hơn tất cả major khác ÍT NHẤT 5 điểm (trừ khi lý do mạnh mẽ ngược lại, nhưng hiếm).
                    - Lý do (reasons) 1–3 câu, viện dẫn rõ career_goal và nội dung major; không phóng đại.

                    ĐẦU RA: CHỈ JSON theo schema đã cho, không thêm văn xuôi.
                    """;
                    string user =
                        $@"Yêu cầu:
                - career_goal: {req.CareerGoal}
                - known_frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}
                - known_languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}

                Major để đánh giá:
                - code: {mj.MajorCode}
                - name: {mj.MajorName}
                - description: {TrimLen(mj.Description, 4000)}

                Hãy trả JSON đúng schema sau (KHÔNG văn xuôi kèm theo):
                {{
                  ""major_code"": ""{mj.MajorCode}"",
                  ""major_name"": ""{mj.MajorName}"",
                  ""supports"": true|false,
                  ""support_score"": <số nguyên 0-100>,
                  ""reasons"": ""<mô tả ngắn gọn 1-3 câu vì sao (hoặc vì sao không)>""
                }}";

                    ChatCompletion completion = await _chat.CompleteChatAsync(
                        new List<ChatMessage>
                        {
                    new SystemChatMessage(sys),
                    new UserChatMessage(user)
                        },
                        cancellationToken: ct
                    );

                    string content = completion.Content.Count > 0 ? completion.Content[0].Text : "{}";
                    var parsed = TryParse<MajorEvaluation>(content)
                                 ?? new MajorEvaluation
                                 {
                                     MajorCode = mj.MajorCode,
                                     MajorName = mj.MajorName,
                                     Supports = false,
                                     SupportScore = 0,
                                     Reasons = "Không thể phân tích kết quả."
                                 };

                    parsed.MajorCode = string.IsNullOrWhiteSpace(parsed.MajorCode) ? mj.MajorCode : parsed.MajorCode;
                    parsed.MajorName = string.IsNullOrWhiteSpace(parsed.MajorName) ? mj.MajorName : parsed.MajorName;
                    parsed.SupportScore = Math.Clamp(parsed.SupportScore, 0, 100);

                    evals.Add(parsed);
                }

                // 4) Lọc matched
                var matched = evals
                    .Where(e => e.Supports && e.SupportScore >= threshold)
                    .OrderByDescending(e => e.SupportScore)
                    .Take(k)
                    .ToList();

                // 5) Gợi ý external tracks nếu có "uncovered"
                var matchedCodes = new HashSet<string>(matched.Select(m => m.MajorCode), StringComparer.OrdinalIgnoreCase);

                // Nội dung đã cover bởi internal
                string coveredText;
                if (matchedCodes.Count > 0)
                {
                    var matchedMajors = majors.Where(m => matchedCodes.Contains(m.MajorCode));
                    coveredText = string.Join(' ', matchedMajors.Select(m => $"{m.MajorName} {m.Description}"));
                }
                else
                {
                    // Fallback: chưa có matched → ưu tiên targets, nếu vẫn rỗng thì dùng toàn bộ majors
                    var baseSet = targets.Count > 0 ? targets : majors;
                    coveredText = string.Join(' ', baseSet.Select(m => $"{m.MajorName} {m.Description}"));
                }
                coveredText = (coveredText ?? string.Empty).ToLowerInvariant();

                // Từ khóa mong muốn: known tech + token từ career_goal
                var cgKeywords = Regex.Matches(req.CareerGoal ?? "", @"[\p{L}A-Za-z0-9\+\#\.]{3,}")
                                      .Cast<Match>()
                                      .Select(m => m.Value.ToLowerInvariant().Trim())
                                      .Distinct()
                                      .ToList();

                var desired = kf.Concat(kl)
                                .Select(s => s.Trim().ToLowerInvariant())
                                .Concat(cgKeywords)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .Distinct()
                                .ToList();

                // uncovered = phần chưa được cover trong internal
                var uncovered = desired.Where(x => !coveredText.Contains(x)).ToList();

                // Luôn generate external (kể cả khi uncovered rỗng)
                var externalSuggestions = new List<ExternalSuggestion>();
                {
                    var timeLimitText = string.IsNullOrWhiteSpace(req.externalLimitTime)
                        ? "120 giờ"
                        : req.externalLimitTime.Trim();

                    // Tóm lược internal matched cho mô hình (nếu có)
                    var internalBrief = matched.Count > 0
                        ? string.Join("; ", matched.Select(e => $"{e.MajorName} (score {e.SupportScore})"))
                        : "(chưa có)";

                    string sys = """
                    Bạn là chuyên gia thiết kế chương trình đào tạo CNTT.
                    Mục tiêu: Đề xuất 1–3 track HỖ TRỢ (external) để BÙ LỖ HỔNG cho lộ trình nội bộ (internal) đã match,
                    hoặc TỰ THIẾT KẾ từ thông tin hiện có nếu chưa có internal match.

                    NGUYÊN TẮC:
                    - Chỉ dùng TIẾNG VIỆT.
                    - Trả về MẢNG JSON HỢP LỆ (1–3 phần tử). KHÔNG thêm văn bản ngoài JSON.
                    - External phải TẬP TRUNG vào các "khoảng trống" (uncovered) — công nghệ/kỹ năng/chủ đề CHƯA được cover bởi internal.
                    - Tránh trùng lặp nội dung với internal; ưu tiên tính BỔ TRỢ, CỦNG CỐ, hoặc HOÀN THIỆN kỹ năng thiếu.
                    - "major_code": HOA, UPPER_SNAKE_CASE, 3–12 ký tự (vd: "GENAI_NLP", "CLOUD_DEVOPS").
                    - "major_name": ngắn gọn (≤ 60 ký tự).
                    - "why_for_you": 1–2 câu, giải thích vì sao track này BÙ LỖ HỔNG tốt cho người học.

                    RÀNG BUỘC THỜI GIAN (GIỜ):
                    - Biến: external_limit_hours (TỔNG SỐ GIỜ tối đa của toàn bộ track).
                    - Tổng giờ của track PHẢI ≤ external_limit_hours. Nếu trống, mặc định ≤ 120 giờ.
                    - Nếu external_limit_hours là khoảng (vd: "20–40 giờ" hay "<= 60 giờ"), dùng CẬN TRÊN làm giới hạn.

                    YÊU CẦU CHO "description":
                    - **MỘT ĐOẠN VĂN NGẮN (1–3 câu)**, KHÔNG markdown/không gạch đầu dòng.
                    - Phải tự nhiên để ghép thẳng vào câu: "tôi muốn lộ trình về " + description.
                    - Nội dung nên gồm: mục tiêu/miền, 2–3 công nghệ/chủ đề TRỌNG TÂM thuộc nhóm uncovered; 2–3 giai đoạn tóm tắt (ngăn cách “;”); kết quả đầu ra.
                    - **BẮT BUỘC kết thúc** bằng cụm: **TỔNG THỜI LƯỢNG: <H> giờ** (chỉ giờ, KHÔNG in tuần/tháng).

                    KIỂM TRA HỢP LÝ:
                    - Nếu danh sách uncovered trống nhưng có internal match, external được phép:
                      • đào sâu “khoảng thiếu chiều sâu” (advanced patterns, optimization, deployment, MLOps, testing, security), hoặc
                      • mở rộng "liên đới thiết yếu" (data, cloud, tooling) MIỄN là thực sự bổ trợ, không trùng.
                    - Nếu KHÔNG có internal match, external phải bám career_goal + known tech.
                    """;


                    string user =
                    $@"Ngữ cảnh người học:
                    - career_goal: {req.CareerGoal}
                    - known_frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}
                    - known_languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}
                    - external_limit_hours: {timeLimitText}

                    Internal (đã cover):
                    {internalBrief}

                    Các KHOẢNG TRỐNG cần bù (uncovered - bắt nguồn từ công nghệ/ngôn ngữ/keyword chưa xuất hiện trong internal):
                    {(uncovered.Count == 0 ? "(chưa xác định rõ — hãy chọn hướng bổ trợ hợp lý như nâng cao/triển khai/bảo mật/kiểm thử/MLOps…)" : string.Join(", ", uncovered))}

                    Hãy TRẢ VỀ MẢNG JSON (1–3 phần tử), schema:
                    {{
                      ""major_code"": ""<VIẾT HOA, UPPER_SNAKE_CASE, 3–12 ký tự>"",
                      ""major_name"": ""<tên track ngắn gọn, ≤ 60 ký tự>"",
                      ""description"": ""<MỘT ĐOẠN VĂN; KẾT THÚC bằng 'TỔNG THỜI LƯỢNG: H giờ'; phù hợp để ghép vào 'tôi muốn lộ trình về ...'>"",
                      ""why_for_you"": ""<1–2 câu, nhấn mạnh vai trò BÙ LỖ HỔNG so với internal>""
                    }}";


                    ChatCompletion completion = await _chat.CompleteChatAsync(
                        new List<ChatMessage> { new SystemChatMessage(sys), new UserChatMessage(user) },
                        new ChatCompletionOptions { Temperature = 0.3f },
                        cancellationToken: ct
                    );
                    string content = completion.Content.Count > 0 ? completion.Content[0].Text : "[]";
                    externalSuggestions = TryParse<List<ExternalSuggestion>>(content) ?? new List<ExternalSuggestion>();
                }

                return new EvaluateResult
                {
                    Inputs = new
                    {
                        career_goal = req.CareerGoal,
                        known_frameworks = kf,
                        known_languages = kl,
                        score_threshold = threshold,
                        k_retrieval = k
                    },
                    Evaluations = evals,
                    Matched = matched,
                    ExternalSuggestions = externalSuggestions,
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Error " + e.Message);
                throw;
            }
        }

        // Helpers
        private static string TrimLen(string? s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max]);

        private static T? TryParse<T>(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return default;
            string s = raw.Trim();

            int objStart = s.IndexOf('{'); int objEnd = s.LastIndexOf('}');
            int arrStart = s.IndexOf('['); int arrEnd = s.LastIndexOf(']');

            string candidate = s;
            if (arrStart >= 0 && arrEnd > arrStart) candidate = s.Substring(arrStart, arrEnd - arrStart + 1);
            else if (objStart >= 0 && objEnd > objStart) candidate = s.Substring(objStart, objEnd - objStart + 1);

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return System.Text.Json.JsonSerializer.Deserialize<T>(candidate, opts);
            }
            catch { return default; }
        }
        public async Task<AskResponse> AskAsync(string question, int k, bool showSources, CancellationToken ct)
        {
            var limitWeeks = ExtractLimitWeeksFromTextOrHours(question) ?? 24;
            // 1) Embed & retrieve như cũ
            var embRes = await _embed.GenerateEmbeddingAsync(question, cancellationToken: ct);
            var qvecArr = embRes.Value.ToFloats().ToArray();
            var docsRaw = await _search.SearchCoursesTopKAsync(qvecArr, question, k <= 0 ? 40 : k, ct);
            if (docsRaw.Count == 0) return new AskResponse { Answer = "Không thấy khóa tương ứng trong CSDL hiện có." };

            // Ưu tiên những doc có EstimatedWeeks <= limitWeeks
            var annotated = docsRaw
                .Select(d => new { Doc = d, Weeks = EstimateWeeksFromDoc(d) })
                .ToList();

            var filtered = annotated
                .Where(x => !x.Weeks.HasValue || x.Weeks.Value <= limitWeeks)
                .Select(x => x.Doc)
                .ToList();

            // Nếu lọc xong trống, dùng lại docsRaw; còn không thì dùng filtered
            var docs = filtered.Count > 0 ? filtered : docsRaw;
            // 2) Build context
            var context = JoinContext(docs, Math.Min(k, 40));

            // 3) ÉP JSON-ONLY với schema rõ ràng
            var sys = $@"
Bạn là trợ lý học tập. Chỉ dùng THÔNG TIN trong phần 'Dữ liệu' để trả lời bằng tiếng Việt.
TRẢ LỜI CHỈ BẰNG JSON HỢP LỆ (UTF-8), KHÔNG THÊM VĂN BẢN NGOÀI JSON.

MỤC TIÊU & THUẬT NGỮ LÕI:
- Từ nội dung 'Câu hỏi', hãy trích ra CORE_TERMS = tập các thuật ngữ/stack/domain bắt buộc (ví dụ: Node.js, JavaScript, Express, React, .NET, Java, Python, TensorFlow, PyTorch, MLOps, Deployment, REST API...).
- Mọi đề xuất phải BÁM SÁT CORE_TERMS; chỉ chọn tài liệu/khóa học liên quan trực tiếp.

TIMEBOX BẮT BUỘC:
- Tổng thời lượng của toàn bộ lộ trình (tổng 'duration_weeks' các step) PHẢI ≤ {limitWeeks}.
- Mỗi suggested_course PHẢI có 'est_duration_weeks' (số nguyên, ước lượng từ dữ liệu).
- Chỉ chọn khóa có est_duration_weeks ≤ duration_weeks của step chứa nó.
- Nếu một khóa là Specialization dài hơn step, hãy chọn MỘT học phần/module/phần tử con phù hợp (nếu có trong dữ liệu) hoặc bỏ qua.
- Ưu tiên các mục có 'DurationHintWeeks' phù hợp với step.

RÀNG BUỘC TÍNH LIÊN QUAN (RẤT QUAN TRỌNG):
- Định nghĩa LIÊN QUAN: (tiêu đề hoặc nội dung tóm tắt trong 'Dữ liệu' hoặc URL hoặc provider) chứa ÍT NHẤT MỘT phần tử của CORE_TERMS.
- ÍT NHẤT 60% tổng số 'suggested_courses' trong toàn lộ trình phải LIÊN QUAN theo định nghĩa trên.
- Nếu CORE_TERMS chứa 'Node.js' hoặc 'JavaScript' hoặc 'Express' thì ÍT NHẤT 2 khóa phải nhắc trực tiếp đến 'Node.js'/'JavaScript'/'Express'.
- Nếu CORE_TERMS chứa 'TensorFlow' hoặc 'PyTorch', thì ÍT NHẤT 1 khóa phải nhắc trực tiếp đến framework đó.
- KHÔNG chọn khóa thiên về 'data analysis' chung chung hoặc công cụ khác stack nếu không phục vụ trực tiếp mục tiêu của step.
- Tránh khóa chỉ dạy Python/Flask khi CORE_TERMS yêu cầu Node.js/JS/Express, trừ khi minh họa nguyên tắc chuyển đổi; tối đa 1 khóa ngoại lệ như vậy.
- Nếu không tìm thấy khóa phù hợp cho một step, để 'suggested_courses' rỗng thay vì chèn khóa không liên quan.

CHẤT LƯỢNG LỘ TRÌNH:
- Mỗi 'step' phải có tiêu đề phản ánh trực tiếp CORE_TERMS và mục tiêu của step.
- Mỗi 'reason' phải nêu rõ: {"khóa này hỗ trợ CORE_TERMS nào và mục tiêu nào của step"}.
- Loại bỏ trùng lặp theo tiêu đề/URL/provider. Tổng số khóa toàn lộ trình ≤ 5.

Schema JSON bắt buộc:
{{
  ""roadmap_title"": string,
  ""steps"": [
    {{
      ""title"": string,
      ""duration_weeks"": number,
      ""objectives"": [string],
      ""suggested_courses"": [
        {{
          ""title"": string,
          ""link"": string,
          ""provider"": string,
          ""reason"": string,
          ""level"": string,
          ""rating"": string,
          ""est_duration_weeks"": number
        }}
      ]
    }}
  ]
}}

Ràng buộc khác:
- Không dùng bảng Markdown. Không in chữ ngoài JSON.
- ""link"" lấy từ metadata.url nếu có; nếu không có hãy bóc link đầu tiên trong content.
- Tối đa 5 khóa học toàn bộ.
";

            var user = $"Câu hỏi: {question}\n\nDữ liệu:\n{context}";

            var completion = await _chat.CompleteChatAsync(
                new List<ChatMessage> {
            new SystemChatMessage(sys),
            new UserChatMessage(user)
                },
                cancellationToken: ct
            );

            var raw = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : "";

            // 4) Cố gắng "bắt" JSON & parse (robust)
            var json = ExtractJsonBlock(raw);
            RoadmapPayload? roadmap = TryDeserialize<RoadmapPayload>(json);

            // 5) Fallback: nếu JSON lỗi thì trả văn bản như cũ (đã loại bảng)
            if (roadmap == null)
            {
                var answer = NormalizeNoTables(raw);
                return new AskResponse { Answer = answer };
            }

            return new AskResponse
            {
                Answer = "",   // vì đã có JSON structured
                Roadmap = roadmap,
            };
        }
        private static T? TryDeserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(json, opts);
            }
            catch { return default; }
        }

        /// <summary>
        /// Get Json
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string ExtractJsonBlock(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim();

            // Ưu tiên mảng/đối tượng lớn nhất
            int objStart = s.IndexOf('{'); int objEnd = s.LastIndexOf('}');
            if (objStart >= 0 && objEnd > objStart) return s.Substring(objStart, objEnd - objStart + 1);

            int arrStart = s.IndexOf('['); int arrEnd = s.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart) return s.Substring(arrStart, arrEnd - arrStart + 1);

            return s; // để TryDeserialize tự fail nếu không phải JSON
        }

        /// <summary>
        /// Extracts week duration from Vietnamese text using regex with diacritic support.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns>Number of weeks extracted or null if no patterns found</returns>
        private static int? ExtractWeekLimitFromText(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return null;
            var t = txt.ToLowerInvariant();

            // Ưu tiên pattern "TỔNG THỜI LƯỢNG: 10 tuần"
            var m1 = Regex.Match(t, @"t(ô|o)̉ng\s+th(ơ|o)̀i\s+l(ư|u)ợng\s*:\s*(\d{1,3})\s*tu(â|a)̀n");
            if (m1.Success && int.TryParse(m1.Groups[4].Value, out var w1)) return w1;

            // fallback "10 tuần"
            var m2 = Regex.Match(t, @"(\d{1,3})\s*tu(â|a)̀n");
            if (m2.Success && int.TryParse(m2.Groups[1].Value, out var w2)) return w2;

            // "X tháng" → 4 tuần/tháng
            var m3 = Regex.Match(t, @"(\d{1,2})\s*th(á|a)ng");
            if (m3.Success && int.TryParse(m3.Groups[1].Value, out var mon)) return mon * 4;

            return null;
        }

        /// <summary>
        ///  Estimates the number of weeks from text content by analyzing various time patterns. Use for English response
        /// </summary>
        /// <param name="content">Text content to analyze for duration information</param>
        /// <returns>Estimated weeks or null if no duration patterns found</returns>
        private static int? EstimateWeeksFromText(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            var c = content.ToLowerInvariant();

            // "X weeks"
            var mw = Regex.Match(c, @"(\d{1,3})\s*week");
            if (mw.Success && int.TryParse(mw.Groups[1].Value, out var w)) return w;

            // "X months" → 4 tuần/tháng
            var mm = Regex.Match(c, @"(\d{1,2})\s*month");
            if (mm.Success && int.TryParse(mm.Groups[1].Value, out var m)) return m * 4;

            // "at N hours a week" + "T total hours" → ceil(T/N)
            var mhpw = Regex.Match(c, @"(\d{1,3})\s*hours?\s*(per|a)\s*week");
            var mth = Regex.Match(c, @"(\d{1,4})\s*total\s*hours|\b(\d{1,4})\s*hours\b");
            if (mhpw.Success && mth.Success)
            {
                var hpw = int.Parse(mhpw.Groups[1].Value);
                var th = int.Parse(string.IsNullOrEmpty(mth.Groups[1].Value) ? mth.Groups[2].Value : mth.Groups[1].Value);
                var est = (int)Math.Ceiling((double)th / Math.Max(hpw, 1));
                if (est > 0) return est;
            }

            // guided project → 1 tuần
            if (c.Contains("guided project")) return 1;

            return null;
        }

        private static int? EstimateWeeksFromDoc(DocumentDto d)
        {
            try
            {
                if (d.Metadata.ValueKind == JsonValueKind.Object &&
                    d.Metadata.TryGetProperty("duration_weeks", out var jw) &&
                    jw.ValueKind == JsonValueKind.Number &&
                    jw.TryGetInt32(out var metaWeeks))
                {
                    return metaWeeks;
                }
            }
            catch { /* ignore */ }

            // parse từ content + metadata text
            string metaRaw = "";
            try { metaRaw = d.Metadata.GetRawText(); } catch { /* ignore */ }
            return EstimateWeeksFromText($"{d.Content}\n{metaRaw}");
        }
        private static string JoinContext(IReadOnlyList<DocumentDto> docs, int maxDocs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(maxDocs, docs.Count); i++)
            {
                var d = docs[i];
                var md = d.Metadata;

                string title = md.TryGetProperty("title", out var jt) ? jt.GetString() ?? "" : "";
                string org = md.TryGetProperty("organization", out var jo) ? jo.GetString() ?? "" : "";
                string level = md.TryGetProperty("level", out var jl) ? jl.GetString() ?? "" : "";
                string rating = md.TryGetProperty("rating", out var jr) ? jr.ToString() : "";
                string numReviews = md.TryGetProperty("num_reviews", out var jn) ? jn.ToString() : "";
                string url = GetUrl(md, d.Content);
                int? weeks = EstimateWeeksFromDoc(d);
                string weeksHint = weeks.HasValue ? $" | DurationHintWeeks: {weeks.Value}" : "";

                sb.AppendLine($"[{i + 1}] Title: {title}");
                sb.AppendLine($"Provider: {org}");
                sb.AppendLine($"Level: {level} | Rating: {rating} | Reviews: {numReviews}{weeksHint}");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine();
                sb.AppendLine(d.Content);
                sb.AppendLine();
                if (i < maxDocs - 1) sb.AppendLine("-----");
            }
            return sb.ToString().Trim();
        }

        private static readonly Regex UrlRe = new(@"\bhttps?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string GetUrl(JsonElement md, string content)
        {
            if (md.ValueKind == JsonValueKind.Object &&
                md.TryGetProperty("url", out var ju) &&
                ju.ValueKind == JsonValueKind.String)
            {
                var u = ju.GetString();
                if (!string.IsNullOrWhiteSpace(u)) return u!.Trim();
            }

            if (!string.IsNullOrEmpty(content))
            {
                var m = UrlRe.Match(content);
                if (m.Success) return m.Value.TrimEnd(')', '.', ',');
            }
            return "";
        }
        private static string NormalizeNoTables(string text)
        {
            var lines = text.Split('\n');
            var outLines = new List<string>();
            foreach (var ln in lines)
            {
                var s = ln.TrimEnd();
                if (s.StartsWith("|") && s.EndsWith("|")) continue;
                if (Regex.IsMatch(s, @"^[-:\s|]{3,}$")) continue;
                outLines.Add(ln);
            }
            return string.Join('\n', outLines).Trim();
        }

        private static int? ExtractLimitWeeksFromTextOrHours(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return null;
            var t = txt.ToLowerInvariant();

            // 1) Giờ: "123 giờ"
            var mh = Regex.Match(t, @"(\d{1,4})\s*gi(?:ơ|o)̀?");
            if (mh.Success && int.TryParse(mh.Groups[1].Value, out var h))
            {
                return (int)Math.Ceiling(h / (double)HOURS_PER_WEEK);
            }

            // 2) Tuần (tương thích cũ)
            var m1 = Regex.Match(t, @"t(ô|o)̉ng\s+th(ơ|o)̀i\s+l(ư|u)ợng\s*:\s*(\d{1,3})\s*tu(â|a)̀n");
            if (m1.Success && int.TryParse(m1.Groups[4].Value, out var w1)) return w1;

            var m2 = Regex.Match(t, @"(\d{1,3})\s*tu(â|a)̀n");
            if (m2.Success && int.TryParse(m2.Groups[1].Value, out var w2)) return w2;

            // 3) Tháng → 4 tuần/tháng (tương thích cũ)
            var m3 = Regex.Match(t, @"(\d{1,2})\s*th(á|a)ng");
            if (m3.Success && int.TryParse(m3.Groups[1].Value, out var mon)) return mon * 4;

            return null;
        }
    }
}
