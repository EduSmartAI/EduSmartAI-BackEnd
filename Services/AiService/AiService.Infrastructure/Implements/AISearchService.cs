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
        private readonly ChatClient _chat;                // OpenAI (đã bind model qua DI)
        private readonly TavilyClient _tavily;            // Tavily .NET SDK
        private readonly string _tavilyKey;               // Lấy từ IOptions<TavilyOptions>
        private readonly bool _fastModeDefault;
        private readonly List<string> _defaultTrusted;
        private readonly List<string> _defaultAvoid;

        // Tuning flags (match Python)
        private const int MAX_SEEDS_FAST = 8;
        private const int MAX_RESULTS_PER_QUERY_FAST = 3;  // giới hạn sau khi lấy về
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

        private static readonly JsonSerializerOptions JSO = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public AISearchService(
            ChatClient chat,
            TavilyClient tavily,
            IConfiguration config
        )
        {
            _chat = chat;
            _tavily = tavily;
            _tavilyKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY") ?? string.Empty;
            var fastStr = config["WebResearch:FastMode"];
            _fastModeDefault = bool.TryParse(fastStr, out var b) ? b : true;
            _defaultTrusted = config.GetSection("WebResearch:DefaultTrustedDomains").Get<string[]>()?.ToList()
                      ?? TRUSTED_FALLBACK.ToList();
            _defaultAvoid = config.GetSection("WebResearch:DefaultAvoidDomains").Get<string[]>()?.ToList()
                            ?? AVOID_FALLBACK.ToList();
        }
        private static bool LooksLikePromoOrSyllabus(string? title, string? url)
        {
            var t = title ?? "";
            var u = url ?? "";
            var hay = (t + " " + u).ToLowerInvariant();

            // syllabus/training/bootcamp/enroll/fees
            if (Regex.IsMatch(hay, @"\b(syllabus|training|boot\s*camp|enrol|enroll|tuition|fee|fees)\b", RegexOptions.IgnoreCase))
                return true;

            // PDF chỉ chấp nhận nếu thuộc domain tin cậy
            if (u.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var h = DomainOf(u);
                if (!TRUSTED_FALLBACK.Contains(h, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
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

            var rawItems = await TavilySearchParallelAsync(
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

        // ---------------- Tavily search (song song) ----------------

        private async Task<List<TavilyItem>> TavilySearchParallelAsync(
    List<string> queries,
    HashSet<string> trustedDomains,
    HashSet<string> avoidDomains)
        {
            var tasks = queries.Select(async q =>
            {
                try
                {
                    var res = await _tavily.SearchAsync(apiKey: _tavilyKey, query: q);
                    var results = res?.Results;
                    if (results == null || results.Count == 0)
                        return new List<TavilyItem>();

                    var list = new List<TavilyItem>();
                    foreach (var r in results)
                    {
                        var host = DomainOf(r.Url);

                        // Loại domain né + link rác (syllabus/quảng cáo/PDF ngoài whitelist)
                        if (IsAvoid(host, avoidDomains) || LooksLikePromoOrSyllabus(r.Title, r.Url))
                            continue;

                        double score = r.Score; // Score của Tavily là double (không dùng ??)

                        // Boost domain tin cậy
                        if (IsTrusted(host, trustedDomains)) score += 0.15;
                        if (host.Equals("react.dev", StringComparison.OrdinalIgnoreCase)) score += 0.20;
                        if (host.Equals("redux.js.org", StringComparison.OrdinalIgnoreCase)) score += 0.10;
                        if (host.Equals("developer.mozilla.org", StringComparison.OrdinalIgnoreCase)) score += 0.05;

                        // Nếu query hướng video thì ưu tiên youtube khi thích hợp
                        if ((q.Contains("video", StringComparison.OrdinalIgnoreCase) || q.Contains("playlist", StringComparison.OrdinalIgnoreCase)) &&
                            (host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)))
                        {
                            score += 0.05;
                        }

                        list.Add(new TavilyItem
                        {
                            Title = r.Title,
                            Content = r.Content,
                            Url = r.Url,
                            Score = score,
                            Host = host
                        });
                    }

                    return list;
                }
                catch
                {
                    return new List<TavilyItem>();
                }
            });

            var all = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();

            // sort + dedup + limit per domain
            all.Sort((a, b) => b.Score.CompareTo(a.Score));
            var dedup = DedupResults(all, limitPerDomain: 2);

            return dedup.Take(Math.Max(MAX_RESULTS_PER_QUERY_FAST * Math.Max(1, queries.Count), 30)).ToList();
        }

        private static List<TavilyItem> DedupResults(List<TavilyItem> items, int limitPerDomain)
        {
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var perDomain = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var res = new List<TavilyItem>();

            foreach (var it in items)
            {
                var url = NormalizeUrl(it.Url ?? "");
                if (url.Length == 0 || seenUrls.Contains(url)) continue;

                var d = it.Host ?? "";
                perDomain.TryGetValue(d, out var c);
                if (c >= limitPerDomain) continue;

                perDomain[d] = c + 1;
                seenUrls.Add(url);
                res.Add(it);
            }
            return res;
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

        private static string NormalizeUrl(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return "";
            try
            {
                var p = new Uri(u);
                var path = p.AbsolutePath.TrimEnd('/');
                return $"{p.Scheme}://{p.Host}{path}";
            }
            catch { return u!.Trim(); }
        }

        private static string DomainOf(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return "";
            try
            {
                return new Uri(u).Host.ToLowerInvariant();
            }
            catch { return ""; }
        }

        private static bool IsTrusted(string host, HashSet<string> trusted)
        {
            if (string.IsNullOrEmpty(host)) return false;
            return trusted.Contains(host) || trusted.Any(d => host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAvoid(string host, HashSet<string> avoid)
        {
            if (string.IsNullOrEmpty(host)) return false;
            return avoid.Contains(host) || avoid.Any(d => host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }

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

        private sealed class TavilyItem
        {
            public string? Title { get; set; }
            public string? Content { get; set; }
            public string? Url { get; set; }
            public double Score { get; set; }
            public string? Host { get; set; }
        }
    }
}
