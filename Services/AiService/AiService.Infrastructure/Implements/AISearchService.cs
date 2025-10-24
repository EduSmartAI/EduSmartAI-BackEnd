using AiService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tavily;


namespace AiService.Infrastructure.Implements
{
    public class AISearchService : IAISearchService
    {
        private readonly ChatClient _chat;
        private readonly bool _fastModeDefault;
        private readonly List<string> _defaultTrusted;
        private readonly List<string> _defaultAvoid;
        private readonly ITavilyService _tavilyService;
        // Tuning flags (match Python)
        private const int MAX_SEEDS_FAST = 8;
        private const int SNIPPET_LEN = 280;

        private static readonly string[] TRUSTED_FALLBACK =
    {
        "react.dev",
        "redux.js.org",
        "overreacted.io",      // blog Dan Abramov
        "kentcdodds.com",
        "ui.dev",
        "developer.mozilla.org"
    };

        private static readonly string[] AVOID_FALLBACK =
        {
        "credosystemz.com",
        "geocities.ws",
        "wisdomsprouts.in"
            };
        private static readonly string[] MCQ_TRUSTED_FALLBACK =
        {
            "geeksforgeeks.org",
            "khanacademy.org",
            "programiz.com",
            "freecodecamp.org",
            "tutorialspoint.com",
            "w3schools.com",
            "javatpoint.com",
            "byjus.com",
            "brilliant.org"
        };

        private static readonly JsonSerializerOptions JSO = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public AISearchService(
            ChatClient chat,
            TavilyClient tavily,
            IConfiguration config,
            ITavilyService tavilyService
        )
        {
            _chat = chat;
            var fastStr = config["WebResearch:FastMode"];
            _fastModeDefault = bool.TryParse(fastStr, out var b) ? b : true;
            _defaultTrusted = config.GetSection("WebResearch:DefaultTrustedDomains").Get<string[]>()?.ToList()
                      ?? TRUSTED_FALLBACK.ToList();
            _defaultAvoid = config.GetSection("WebResearch:DefaultAvoidDomains").Get<string[]>()?.ToList()
                            ?? AVOID_FALLBACK.ToList();
            _tavilyService = tavilyService;
        }
        #region
        public async Task<string> FindCourseResourcesAsync(string topic, string audience, bool? fastModeOverride)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return "# (không có chủ đề)\n\nVui lòng nhập chủ đề cụ thể.";

            var useFast = fastModeOverride ?? _fastModeDefault;

            // 1) Xây seeds
            List<string> seeds;
            Dictionary<string, object?>? meta = null;
            if (useFast)
            {
                seeds = BuildSeedQueriesFast(topic);
            }
            else
            {
                (seeds, meta) = await BuildSeedQueriesAiAsync(topic, audience);
            }

            if (seeds.Count == 0)
                seeds = BuildSeedQueriesFast(topic);

            // 2) Tìm kiếm Tavily song song
            var trusted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var avoid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in _defaultTrusted) trusted.Add(CleanDomain(d));
            foreach (var d in _defaultAvoid) avoid.Add(CleanDomain(d));

            if (meta != null)
            {
                if (meta.TryGetValue("trusted_domains", out var tds) && tds is IEnumerable<object> tdList)
                {
                    foreach (var x in tdList) if (x is string s && !string.IsNullOrWhiteSpace(s)) trusted.Add(CleanDomain(s));
                }
                if (meta.TryGetValue("avoid_domains", out var ads) && ads is IEnumerable<object> adList)
                {
                    foreach (var x in adList) if (x is string s && !string.IsNullOrWhiteSpace(s)) avoid.Add(CleanDomain(s));
                }
            }

            var rawItems = await _tavilyService.TavilySearchParallelAsync(
                seeds,
                trustedDomains: trusted,
                avoidDomains: avoid
            );

            if (rawItems.Count == 0)
                return $"# {topic}\n\nKhông tìm thấy kết quả phù hợp. Hãy mô tả cụ thể hơn hoặc đổi chủ đề.";

            var top = rawItems.Take(10).ToList();

            // 3) Tổng hợp bằng OpenAI (Markdown)
            var md = await SynthesizeWithOpenAIAsync(topic, top, audience);

            // 4) Append danh sách nguồn
            var tail = string.Join("\n", rawItems.Take(12).Select(it =>
            {
                var title = string.IsNullOrWhiteSpace(it.Title) ? "(no title)" : it.Title!.Trim();
                var url = it.Url ?? "";
                return $"- [{EscapeMd(title)}]({url})";
            }));

            return md + "\n\n---\n\n### Nguồn đã thu thập\n" + tail + "\n";
        }

        // ---------------- Seeds ----------------

        private static List<string> BuildSeedQueriesFast(string topic)
        {
            var baseSeeds = new[]
            {
                $"{topic} tutorial",
                $"{topic} guide",
                $"{topic} video playlist",
                $"{topic} crash course video",
                $"{topic} exercises",
                $"{topic} hands-on lab",
                $"{topic} project ideas",
                $"{topic} syllabus filetype:pdf",
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            foreach (var q in baseSeeds)
            {
                var k = q.Trim();
                if (k.Length == 0 || seen.Contains(k)) continue;
                seen.Add(k);
                list.Add(k);
            }
            return list.Take(MAX_SEEDS_FAST).ToList();
        }

        private async Task<(List<string>, Dictionary<string, object?>)> BuildSeedQueriesAiAsync(string topic, string audience)
        {
            var meta = await AiExpandTopicAsync(topic, audience);
            var terms = (meta.TryGetValue("terms", out var t) && t is IEnumerable<object> tl)
                ? tl.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Take(6).ToList()
                : new List<string> { topic };

            var intents = (meta.TryGetValue("intents", out var i) && i is IEnumerable<object> il)
                ? il.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)).Take(4).ToList()
                : new List<string> { "tutorial", "guide", "best practices", "exercises", "reference" };

            var seeds = new List<string>();
            foreach (var term in terms)
            {
                foreach (var intent in intents)
                {
                    seeds.Add($"{term} {intent}");
                }
            }
            seeds.Add($"{terms[0]} overview");
            seeds.Add($"{terms[0]} pitfalls");

            var uniq = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in seeds)
            {
                var k = s.Trim();
                if (k.Length == 0 || seen.Contains(k)) continue;
                seen.Add(k);
                uniq.Add(k);
            }
            return (uniq.Take(MAX_SEEDS_FAST).ToList(), meta);
        }
        // ---------------- OpenAI synthesize ----------------

        private async Task<string> SynthesizeWithOpenAIAsync(string topic, List<TavilyItem> items, string audience)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(10, items.Count); i++)
            {
                var it = items[i];
                var title = string.IsNullOrWhiteSpace(it.Title) ? "(no title)" : it.Title!.Trim();
                var url = it.Url ?? "";
                var snippet = (it.Content ?? string.Empty).Trim();
                snippet = Regex.Replace(snippet, "\\s+", " ");
                if (snippet.Length > SNIPPET_LEN) snippet = snippet[..SNIPPET_LEN];
                sb.AppendLine($"{i + 1}. {title}");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine($"Snippet: {snippet}");
                sb.AppendLine();
            }

            var system =
                "Bạn là trợ lý học tập. Trả lời DUY NHẤT bằng Markdown (ngắn gọn, rõ ràng). " +
                "Chỉ dùng các URL đã cung cấp. Ưu tiên domain chính thống, bỏ qua nguồn kém uy tín.";
            var user = $@"
            Chủ đề: ""{topic}""
            Đối tượng: {audience}

            Yêu cầu xuất ra Markdown theo dàn ý 3 giai đoạn (chỉ dùng URL trong danh sách bên dưới, không thêm link khác):

            # {topic}

            ## Tóm tắt ngắn
            - Viết 3–5 gạch đầu dòng cô đọng về mục tiêu học và kết quả đạt được.

            ## Lộ trình 3 giai đoạn
            ### Giai đoạn 1 — Nền tảng (1–2 tuần)
            - Mục tiêu: nắm vững khái niệm/cốt lõi.
            #### Video (3–5 mục)
            - [Tiêu đề](URL) — *lý do ngắn* _(video/playlist)_
            #### Tài liệu đọc (3–5 mục)
            - [Tiêu đề](URL) — *lý do ngắn* _(doc/reference/article)_
            > Ưu tiên **react.dev**, **redux.js.org**, **developer.mozilla.org**; hạn chế blog kém uy tín.

            ### Giai đoạn 2 — Luyện tập có hướng dẫn (1–2 tuần)
            - Mục tiêu: củng cố kỹ năng qua thực hành.
            - Bài tập/Lab (4–6 mục):
              - [Tiêu đề](URL) — *mục tiêu rèn luyện* _(exercise/lab)_ — *(beginner/intermediate/advanced)*

            ### Giai đoạn 3 — Dự án tự luyện thực tế (1–2 tuần)
            - Mục tiêu: áp dụng end-to-end, tự đánh giá năng lực.
            - Gợi ý 2–3 đề bài (mô tả + tiêu chí chấp nhận).
            - Tài nguyên tham khảo (3–5 mục):
              - [Tiêu đề](URL) — *liên hệ với yêu cầu dự án* _(reference/template/article/video)_

            ## Top resources (xếp hạng 6–8 mục)
            1. [Tiêu đề](URL) — *vì sao đáng dùng* _(level: beginner/intermediate/advanced; loại: …)_
            2. …

            ## Mẹo & Từ khóa
            - Mẹo: 2–4 mẹo thực tiễn (debug, tối ưu, checklist).
            - Từ khóa: liệt kê 8–12 từ/cụm từ quan trọng.

            Quy tắc:
            - Nếu không đủ link phù hợp cho một mục, vẫn giữ mục và ghi: *(chưa tìm thấy link phù hợp trong danh sách)*.
            - Tuyệt đối không thêm URL ngoài danh sách.
            === WEB RESULTS (chỉ để chọn link) ===
            {sb}
            ".Trim();

            // Dùng 1 prompt (gộp system + user) để đảm bảo tương thích API (đỡ lệ thuộc kiểu message)
            var prompt = $"{system}\n\n{user}";
            try
            {
                var completion = await _chat.CompleteChatAsync(prompt);
                var text = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text?.Trim() : null;
                if (!string.IsNullOrWhiteSpace(text))
                    return text!;
            }
            catch
            {
                // ignore
            }
            return $"# {topic}\n\n_(Không lấy được nội dung tổng hợp. Hãy chạy lại.)_";
        }

        // ---------------- AI expand topic → terms/intents/domains ----------------

        private async Task<Dictionary<string, object?>> AiExpandTopicAsync(string topic, string audience)
        {
            // prompt giống Python
            var sys = "Bạn là trợ lý nghiên cứu web. Hãy mở rộng chủ đề thành các từ khóa, ý định tìm kiếm và danh sách miền uy tín. Trả lời *chỉ* JSON hợp lệ, không thêm markdown hay lời giải thích.";
            var user = $@"
            Chủ đề: ""{topic}""
            Đối tượng: {audience}
            Yêu cầu JSON schema:
            {{
              ""terms"": [""...""],            // 6-14 terms
              ""intents"": [""...""],          // 6-12 intents
              ""trusted_domains"": [""...""],  // 6-16 domain uy tín
              ""avoid_domains"": [""...""]     // 0-10 domain nên tránh
            }}
            Quy tắc:
            - Ưu tiên domain chính thống (docs chính thức, tiêu chuẩn/spec, cổng công nghệ lớn).
            - Không bịa domain; chỉ đưa domain có thật.
            - Không lặp.
            - Trả về JSON hợp lệ duy nhất (không bọc markdown).
            ".Trim();

            var prompt = $"{sys}\n\n{user}";
            string raw = "";
            try
            {
                var c = await _chat.CompleteChatAsync(prompt);
                raw = c.Value.Content.Count > 0 ? (c.Value.Content[0].Text ?? "") : "";
            }
            catch { /* ignore */ }

            // cố gắng trích JSON
            var data = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var m = Regex.Match(raw, "\\{(?:.|\\n)*\\}");
                if (m.Success)
                {
                    try
                    {
                        data = JsonSerializer.Deserialize<Dictionary<string, object?>>(m.Value, JSO) ?? new();
                    }
                    catch { /* ignore */ }
                }
            }

            // fallback tối thiểu + làm sạch
            if (!data.ContainsKey("terms")) data["terms"] = new List<string> { topic.Trim() };
            if (!data.ContainsKey("intents")) data["intents"] = new List<string> { "tutorial", "guide", "best practices", "exercises", "reference" };
            if (!data.ContainsKey("trusted_domains")) data["trusted_domains"] = new List<string>();
            if (!data.ContainsKey("avoid_domains")) data["avoid_domains"] = new List<string>();

            data["terms"] = CleanListOfString(data["terms"], max: 14);
            data["intents"] = CleanListOfString(data["intents"], max: 12);
            data["trusted_domains"] = CleanDomainList(data["trusted_domains"], max: 16);
            data["avoid_domains"] = CleanDomainList(data["avoid_domains"], max: 10);

            return data;
        }

        // ---------------- Helpers ----------------

        private static string CleanDomain(string d)
        {
            d = d.Trim().ToLowerInvariant();
            if (d.StartsWith("https://")) d = d[8..];
            if (d.StartsWith("http://")) d = d[7..];
            if (d.EndsWith("/")) d = d[..^1];
            return d;
        }

        private static List<string> CleanListOfString(object? obj, int max)
        {
            var list = new List<string>();
            if (obj is IEnumerable<object> arr)
            {
                foreach (var x in arr)
                    if (x is string s && !string.IsNullOrWhiteSpace(s))
                        list.Add(s.Trim());
            }
            return list.Take(max).ToList();
        }

        private static List<string> CleanDomainList(object? obj, int max)
        {
            var list = CleanListOfString(obj, max: max).Select(CleanDomain).ToList();
            return list.Take(max).ToList();
        }

        private static string EscapeMd(string s)
        {
            return s.Replace("[", "\\[").Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)");
        }
        #endregion

        public async Task<string> FindMultipleChoiceExcercises(string topic, int difficultyLevel, bool? fastModeOverride)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return "# (không có chủ đề)\n\nVui lòng nhập chủ đề cụ thể.";

            // Mặc định: DÙNG LLM keywords (theo yêu cầu), nếu LLM fail thì fallback fast
            List<string> seeds;
            List<string> usedKeywords = new();

            // 1) Gọi LLM sinh keywords + domains
            var meta = await AiExpandTopicForMcqAsync(topic, difficultyLevel);
            (seeds, usedKeywords) = BuildMcqSeedsAi(meta, difficultyLevel);

            if (seeds.Count == 0)
            {
                // Fallback nhanh nếu LLM không sinh được
                seeds = new List<string>
        {
            $"{topic} mcq with answers",
            $"{topic} multiple choice questions with explanations",
            $"{topic} quiz with solutions",
            $"{topic} objective questions with answers"
        }.Take(MAX_SEEDS_FAST).ToList();
                usedKeywords = seeds.ToList();
            }

            // 2) Merge trusted/avoid domains: default + LLM + MCQ fallback
            var trusted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var avoid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in _defaultTrusted) trusted.Add(CleanDomain(d));
            foreach (var d in MCQ_TRUSTED_FALLBACK) trusted.Add(CleanDomain(d));
            foreach (var d in _defaultAvoid) avoid.Add(CleanDomain(d));

            if (meta.TryGetValue("trusted_domains", out var tds) && tds is IEnumerable<object> tdList)
                foreach (var x in tdList) if (x is string s && !string.IsNullOrWhiteSpace(s)) trusted.Add(CleanDomain(s));

            if (meta.TryGetValue("avoid_domains", out var ads) && ads is IEnumerable<object> adList)
                foreach (var x in adList) if (x is string s && !string.IsNullOrWhiteSpace(s)) avoid.Add(CleanDomain(s));

            // 3) Tavily song song + lọc MCQ có giải thích
            var raw = await _tavilyService.TavilySearchParallelAsync(seeds, trusted, avoid);
            var mcq = raw.Where(IsLikelyMcqWithExplanations).Take(20).ToList();

            if (mcq.Count == 0)
                return $"# {EscapeMd(topic)}\n\nKhông tìm thấy nguồn trắc nghiệm kèm giải thích phù hợp. Hãy thử đổi từ khóa/mức độ.";

            // 4) Render Markdown (hiện link + snippet + keywords LLM đã dùng)
            return RenderMcqMarkdown(topic, difficultyLevel, mcq, usedKeywords);
        }


        private static string DifficultyToText(int level)
        {
            // 1: Beginner | 2: Intermediate | 3: Advanced (mặc định: Mixed)
            return level switch
            {
                <= 1 => "Beginner",
                2 => "Intermediate",
                >= 3 => "Advanced"
            };
        }


        private async Task<Dictionary<string, object?>> AiExpandTopicForMcqAsync(string topic, int difficultyLevel)
        {
            var level = DifficultyToText(difficultyLevel).ToLowerInvariant(); // beginner/intermediate/advanced

            // Prompt ép trả JSON thuần (không markdown)
            var sys =
                "Bạn là trợ lý nghiên cứu web. Hãy mở rộng chủ đề thành các từ khóa tìm kiếm chuyên cho 'multiple choice questions' có đáp án/giải thích. " +
                "Trả lời CHỈ JSON hợp lệ, không thêm markdown hay chú thích.";

            var user = $@"
            Chủ đề: ""{topic}""
            Mức độ chính: {level}
            Yêu cầu JSON schema:
            {{
              ""terms"": [""...""],           // 6-14 cụm từ hạt giống liên quan đến chủ đề để tìm MCQ
              ""intents"": [""...""],          // 6-12 đuôi truy vấn (ví dụ: ""mcq with answers"", ""quiz with explanations"", ...)
              ""trusted_domains"": [""...""],  // 6-16 domain uy tín (docs chính thức, nền tảng lớn, giáo dục chất lượng)
              ""avoid_domains"": [""...""]     // 0-10 domain nên tránh (spam/quảng cáo/kém chất lượng)
            }}
            Quy tắc:
            - Các intents PHẢI hướng vào MCQ/quiz có 'answers' hoặc 'explanations'.
            - Ưu tiên domain chính thống, không bịa domain, không lặp.
            - Chỉ trả JSON hợp lệ duy nhất.
            ".Trim();

            var prompt = $"{sys}\n\n{user}";
            string raw = "";
            try
            {
                var c = await _chat.CompleteChatAsync(prompt);
                raw = c.Value.Content.Count > 0 ? (c.Value.Content[0].Text ?? "") : "";
            }
            catch { /* ignore */ }

            var data = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var m = Regex.Match(raw, "\\{(?:.|\\n)*\\}");
                if (m.Success)
                {
                    try
                    {
                        data = JsonSerializer.Deserialize<Dictionary<string, object?>>(m.Value, JSO) ?? new();
                    }
                    catch { /* ignore */ }
                }
            }

            // Fallback + làm sạch
            if (!data.ContainsKey("terms")) data["terms"] = new List<string> { topic.Trim() };
            if (!data.ContainsKey("intents")) data["intents"] = new List<string>
    {
        "mcq with answers","multiple choice questions with explanations","quiz with solutions",
        "practice test with answers","objective questions with answers","interview questions mcq"
    };
            if (!data.ContainsKey("trusted_domains")) data["trusted_domains"] = new List<string>();
            if (!data.ContainsKey("avoid_domains")) data["avoid_domains"] = new List<string>();

            data["terms"] = CleanListOfString(data["terms"], max: 14);
            data["intents"] = CleanListOfString(data["intents"], max: 12);

            // Ghép thêm fallback MCQ uy tín
            var trustedRaw = CleanDomainList(data["trusted_domains"], max: 16);
            foreach (var d in MCQ_TRUSTED_FALLBACK)
                if (!trustedRaw.Contains(CleanDomain(d))) trustedRaw.Add(CleanDomain(d));
            data["trusted_domains"] = trustedRaw;

            data["avoid_domains"] = CleanDomainList(data["avoid_domains"], max: 10);
            return data;
        }

        private static (List<string> seeds, List<string> usedKeywords) BuildMcqSeedsAi(Dictionary<string, object?> meta, int difficultyLevel)
        {
            var terms = (meta.TryGetValue("terms", out var t) && t is IEnumerable<object> tl)
                ? tl.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();

            var intents = (meta.TryGetValue("intents", out var i) && i is IEnumerable<object> il)
                ? il.OfType<string>().Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string>();

            var level = DifficultyToText(difficultyLevel).ToLowerInvariant();

            var seeds = new List<string>();
            var used = new List<string>();

            foreach (var term in terms.Take(7))           // tránh nổ query quá nhiều
            {
                foreach (var intent in intents.Take(6))
                {
                    var q = $"{term} {intent} {level}";
                    seeds.Add(q);
                    used.Add($"{term} {intent}");
                }
            }

            // Khử trùng và cắt theo giới hạn nhanh
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniq = new List<string>();
            foreach (var s in seeds)
            {
                var k = s.Trim();
                if (k.Length == 0 || seen.Contains(k)) continue;
                seen.Add(k);
                uniq.Add(k);
            }

            return (uniq.Take(MAX_SEEDS_FAST).ToList(), used.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList());
        }

        private static bool IsLikelyMcqWithExplanations(TavilyItem it)
        {
            var t = (it.Title ?? "").ToLowerInvariant();
            var u = (it.Url ?? "").ToLowerInvariant();
            var c = (it.Content ?? "").ToLowerInvariant();

            var hasMcq = Regex.IsMatch(t + " " + u, @"\b(mcq|multiple\s*choice|quiz|practice\s*test|objective\s*questions?)\b",
                RegexOptions.IgnoreCase);

            var hasAnswers = Regex.IsMatch(t + " " + u + " " + c, @"\b(answers?|solutions?|explanations?|explained|with\s+answers?)\b",
                RegexOptions.IgnoreCase);

            return hasMcq && hasAnswers;
        }

        private static string RenderMcqMarkdown(string topic, int difficultyLevel, IEnumerable<TavilyItem> items, IEnumerable<string> usedKeywords)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Bài tập trắc nghiệm: {EscapeMd(topic)}");
            sb.AppendLine();
            sb.AppendLine($"**Mức độ:** {DifficultyToText(difficultyLevel)}");
            sb.AppendLine();
            sb.AppendLine("## Nguồn kèm lời giải/giải thích");
            sb.AppendLine();

            int i = 0;
            foreach (var it in items)
            {
                i++;
                var title = string.IsNullOrWhiteSpace(it.Title) ? "(no title)" : it.Title!.Trim();
                var url = it.Url ?? "";
                var host = it.Host ?? "";

                var snippet = (it.Content ?? string.Empty).Trim();
                snippet = Regex.Replace(snippet, "\\s+", " ");
                if (snippet.Length > SNIPPET_LEN) snippet = snippet[..SNIPPET_LEN];

                sb.AppendLine($"{i}. [{EscapeMd(title)}]({url}) — *{host}*");
                if (!string.IsNullOrWhiteSpace(snippet))
                    sb.AppendLine($"   - Gợi ý nội dung: {EscapeMd(snippet)}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("### Từ khóa LLM đã dùng");
            foreach (var kw in usedKeywords)
                sb.AppendLine($"- {EscapeMd(kw)}");

            sb.AppendLine();
            sb.AppendLine("### Cách học nhanh");
            sb.AppendLine("- Làm theo *mức độ* từ dễ → khó; ghi lại câu sai và **lý do**.");
            sb.AppendLine("- Mỗi câu: tự giải trước rồi mới đọc **explanation** để rút quy tắc.");
            sb.AppendLine("- Gom nhóm lỗi → checklist ôn nhanh trước set mới.");

            return sb.ToString();
        }
    }
}
