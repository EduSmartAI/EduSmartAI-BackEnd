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
                string allText = string.Join(' ', majors.Select(m => $"{m.MajorName} {m.Description}")).ToLowerInvariant();
                var uncovered = kf.Concat(kl)
                                  .Where(x => !string.IsNullOrWhiteSpace(x) && !allText.Contains(x.Trim().ToLowerInvariant()))
                                  .ToList();

                var externalSuggestions = new List<ExternalSuggestion>();
                if (uncovered.Count > 0)
                {
                    string sys = """
Bạn là chuyên gia thiết kế chương trình đào tạo CNTT.
Mục tiêu: Đề xuất 1–3 chuyên ngành/track MỚI thật sát với career_goal của người học
và tập trung vào các CÔNG NGHỆ/FRAMEWORK nằm trong danh sách "chưa được bao phủ".

RÀNG BUỘC QUAN TRỌNG:
- Chỉ dùng TIẾNG VIỆT.
- Trả về MẢNG JSON HỢP LỆ (1–3 phần tử). KHÔNG thêm văn bản ngoài JSON.
- Không bịa công nghệ. ƯU TIÊN dùng chính xác các mục trong danh sách "chưa được bao phủ"
  (cho phép dùng tên đồng nghĩa/phổ biến, nhưng không thêm công nghệ mới ngoài danh sách).
- Tính phù hợp: ưu tiên track bám SÁT career_goal; sau đó mới đến bổ trợ.
- Không trùng lặp ý tưởng/track.
- "major_code": viết HOA, dạng UPPER_SNAKE_CASE, 3–12 ký tự, gợi từ công nghệ chính (vd: "GENAI_NLP", "CLOUD_DEVOPS").
- "major_name": ngắn gọn (< 60 ký tự), nêu rõ định hướng/miền.
- "why_for_you": 1–2 câu, nêu vì sao hợp với career_goal & nền tảng hiện tại.

YÊU CẦU QUAN TRỌNG VỀ "description":
- Trả về MỘT CHUỖI có cấu trúc nhất quán theo template dưới (không dùng Markdown):
  TÓM TẮT: <1 câu nêu vai trò/mục tiêu và công nghệ chính>.
  ĐẦU VÀO TỐI THIỂU: <tiên quyết ngắn gọn hoặc "Không yêu cầu">.
  LỘ TRÌNH (3 giai đoạn):
  1) <Tên giai đoạn> | <X–Y tuần> | Mục tiêu: <...>. Chủ đề: <...>. Sản phẩm: <...>.
  2) <Tên giai đoạn> | <X–Y tuần> | Mục tiêu: <...>. Chủ đề: <...>. Sản phẩm: <...>.
  3) <Tên giai đoạn> | <X–Y tuần> | Mục tiêu: <...>. Chủ đề: <...>. Sản phẩm: <...>.
  ĐẦU RA/KỸ NĂNG: <kỹ năng, vị trí việc làm, chứng chỉ liên quan nếu có>.
  ĐÁNH GIÁ: <cách đo tiến độ/tiêu chí hoàn thành (vd: bài tập, mini-capstone, rubric)>.
  TỪ KHÓA: <danh sách từ khóa, phân tách bằng dấu phẩy>.

- Thời lượng mỗi giai đoạn thường 4–8 tuần; tổng ≤ 24 tuần trừ khi có lý do rõ ràng.
- Mỗi track chọn TỐI ĐA 3 công nghệ trọng tâm từ "chưa được bao phủ" và lồng ghép vào các giai đoạn.
- Không chèn link. Không dùng bảng Markdown. Không lạm dụng ký tự đặc biệt.
""";

                    string user =
                    $@"Ngữ cảnh người học:
- career_goal: {req.CareerGoal}
- known_frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}
- known_languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}

Các công nghệ/khung CHƯA được bao phủ (bắt buộc phải là trung tâm của track):
{(uncovered.Count == 0 ? "(không có)" : string.Join(", ", uncovered))}

Hãy TRẢ VỀ MẢNG JSON (1–3 phần tử), mỗi phần tử đúng schema:
{{
  ""major_code"": ""<VIẾT HOA, UPPER_SNAKE_CASE, 3–12 ký tự>"",
  ""major_name"": ""<tên track ngắn gọn, ≤ 60 ký tự>"",
  ""description"": ""<theo đúng template đã cho ở trên>"",
  ""why_for_you"": ""<1–2 câu, bám career_goal & nền tảng hiện tại>""
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
            // 1) Embed & retrieve như cũ
            var embRes = await _embed.GenerateEmbeddingAsync(question, cancellationToken: ct);
            var qvecArr = embRes.Value.ToFloats().ToArray();
            var docs = await _search.SearchCoursesTopKAsync(qvecArr, question, k <= 0 ? 40 : k, ct);
            if (docs.Count == 0)
                return new AskResponse { Answer = "Không thấy khóa tương ứng trong CSDL hiện có." };

            // 2) Build context
            var context = JoinContext(docs, Math.Min(k, 40));

            // 3) ÉP JSON-ONLY với schema rõ ràng
            var sys = """
            Bạn là trợ lý học tập. Chỉ dùng THÔNG TIN trong phần 'Dữ liệu' để trả lời bằng **tiếng Việt**.
            TRẢ LỜI CHỈ BẰNG JSON HỢP LỆ (UTF-8), KHÔNG THÊM VĂN BẢN NGOÀI JSON.

            Schema JSON bắt buộc:
            {
              "roadmap_title": string,
              "steps": [
                {
                  "title": string,
                  "duration_weeks": number,
                  "objectives": [string],
                  "suggested_courses": [
                    { "title": string, "link": string, "provider": string, "reason": string }
                  ]
                }
              ],
              "sources": [
                { "title": string, "url": string, "provider": string, "level": string, "rating": string }
              ]
            }

            Ràng buộc:
            - Không dùng bảng Markdown. Không in chữ ngoài JSON.
            - "link"/"url" lấy từ metadata.url nếu có; nếu không có hãy bóc link đầu tiên trong content.
            - Tối đa 5 khóa học toàn bộ.
            - "sources" khớp theo các course đã nêu, không trùng.
            """;

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

            // Optionally: fill sources từ docs nếu LLM không trả
            if (showSources && (roadmap.Sources == null || roadmap.Sources.Count == 0))
            {
                roadmap.Sources = FormatSources(docs).Select(s => new RoadmapSourcePayload
                {
                    Title = s.Title,
                    Url = s.Url,
                    Provider = s.Provider,
                    Level = s.Level,
                    Rating = s.Rating
                }).ToList();
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
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, opts);
            }
            catch { return default; }
        }

        // Lấy phần JSON “xịn” nhất trong output (nếu model vẫn trót in kèm text)
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

                sb.AppendLine($"[{i + 1}] Title: {title}");
                sb.AppendLine($"Provider: {org}");
                sb.AppendLine($"Level: {level} | Rating: {rating} | Reviews: {numReviews}");
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

        private static List<(string Title, string Url, string Provider, string Level, string Rating)>
        FormatSources(IReadOnlyList<DocumentDto> docs)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<(string, string, string, string, string)>();

            foreach (var d in docs)
            {
                var md = d.Metadata;

                string title = md.TryGetProperty("title", out var jt) ? jt.GetString() ?? "(no title)" : "(no title)";
                string url = GetUrl(md, d.Content); // dùng helper GetUrl bạn đã có
                string key = $"{title}|{url}";
                if (seen.Contains(key)) continue;
                seen.Add(key);

                string org = md.TryGetProperty("organization", out var jo) ? jo.GetString() ?? "—" : "—";
                string level = md.TryGetProperty("level", out var jl) ? jl.GetString() ?? "—" : "—";
                string rating = md.TryGetProperty("rating", out var jr)
                                ? (jr.ValueKind == JsonValueKind.String ? jr.GetString()! : jr.ToString())
                                : "—";

                list.Add((title, url, org, level, rating));
            }

            return list;
        }
        private static string NormalizeNoTables(string text)
        {
            var lines = text.Split('\n');
            var outLines = new List<string>();
            foreach (var ln in lines)
            {
                var s = ln.TrimEnd();
                if (s.StartsWith("|") && s.EndsWith("|")) continue;                    // bỏ dòng bảng
                if (Regex.IsMatch(s, @"^[-:\s|]{3,}$")) continue;                      // bỏ separator
                outLines.Add(ln);
            }
            return string.Join('\n', outLines).Trim();
        }
    }
}
