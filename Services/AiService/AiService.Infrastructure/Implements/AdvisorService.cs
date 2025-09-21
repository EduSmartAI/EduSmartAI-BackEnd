using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text.Json;
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
                    string sys = "Bạn là cố vấn hướng nghiệp đại học. Hãy đánh giá mức độ PHÙ HỢP của một ngành học cho sinh viên theo mục tiêu nghề nghiệp và công cụ/ngôn ngữ đã biết. TRẢ LỜI CHỈ BẰNG JSON hợp lệ cho MỖI MAJOR.";
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
                    string sys = "Bạn là chuyên gia thiết kế chương trình đào tạo CNTT. Hãy đề xuất 1-3 chuyên ngành/track mới tập trung vào các công nghệ/khung chưa được bao phủ bởi majors hiện có.";
                    string user =
                        $@"Các công nghệ/khung CHƯA được bao phủ: {string.Join(", ", uncovered)}
                Bối cảnh người học:
                - career_goal: {req.CareerGoal}
                - known_frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}
                - known_languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}

                Trả về MẢNG JSON (1-3 phần tử), mỗi phần tử có dạng:
                {{
                  ""major_code"": ""<VIẾT HOA, gợi ý code ngắn>"",
                  ""major_name"": ""<tên track/major đề xuất>"",
                  ""description"": ""<3-5 câu: trọng tâm kỹ năng, môn học tiêu biểu, đầu ra nghề nghiệp>"",
                  ""why_for_you"": ""<1-2 câu: vì sao hợp với tiêu chí trên>""
                }}";

                    ChatCompletion completion = await _chat.CompleteChatAsync(
                        new List<ChatMessage> { new SystemChatMessage(sys), new UserChatMessage(user) },
                        cancellationToken: ct
                    );
                    string content = completion.Content.Count > 0 ? completion.Content[0].Text : "[]";
                    externalSuggestions = TryParse<List<ExternalSuggestion>>(content) ?? new List<ExternalSuggestion>();
                }

                // 6) Nếu không có matched -> đề xuất major mới
                ProposedMajor? proposed = null;
                if (matched.Count == 0)
                {
                    string sys = "Bạn là cố vấn chương trình đào tạo. Nếu không có major nào phù hợp, hãy đề xuất một major MỚI.";
                    string user =
                        $@"Không tìm thấy major phù hợp cho tiêu chí:
                - career_goal: {req.CareerGoal}
                - known_frameworks: {(kf.Count == 0 ? "None" : string.Join(", ", kf))}
                - known_languages: {(kl.Count == 0 ? "None" : string.Join(", ", kl))}

                Hãy trả JSON (KHÔNG văn xuôi kèm theo):
                {{
                  ""major_code"": ""<CODE NGẮN, CHỮ HOA>"",
                  ""major_name"": ""<tên dễ hiểu>"",
                  ""description"": ""<mô tả 3-5 câu: mục tiêu, kỹ năng cốt lõi, môn tiêu biểu, cơ hội nghề nghiệp>""
                }}";
                    ChatCompletion completion = await _chat.CompleteChatAsync(
                        new List<ChatMessage> { new SystemChatMessage(sys), new UserChatMessage(user) },
                        cancellationToken: ct
                    );
                    string content = completion.Content.Count > 0 ? completion.Content[0].Text : "{}";
                    proposed = TryParse<ProposedMajor>(content);
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
                    ProposedMajor = proposed
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
    }
}
