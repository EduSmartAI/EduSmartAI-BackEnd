using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.UtilityService;
using MassTransit;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text.RegularExpressions;
using Tavily;

namespace AiService.Infrastructure.Implements
{
    public class TavilyService : ITavilyService
    {
        private readonly TavilyClient _tavily;            // Tavily .NET SDK
        private readonly IRequestClient<GetSystemConfigEvent> _requestClient;
        private string? _tavilyKey;                        // Lazy loaded from UtilityService
        private const int MAX_RESULTS_PER_QUERY_FAST = 3;

        private static readonly string[] TRUSTED_FALLBACK =
        {
        "react.dev",
        "redux.js.org",
        "overreacted.io",      // blog Dan Abramov
        "kentcdodds.com",
        "ui.dev",
        "developer.mozilla.org"
        };


        public TavilyService(
            ChatClient chat,
            TavilyClient tavily,
            IConfiguration config,
            IRequestClient<GetSystemConfigEvent> requestClient
        )
        {
            _tavily = tavily;
            _requestClient = requestClient;
        }

        private async Task<string> GetTavilyKeyAsync(CancellationToken cancellationToken = default)
        {
            if (_tavilyKey != null)
                return _tavilyKey;

            try
            {
                var request = new GetSystemConfigEvent { ConfigId = "TAVILY_API_KEY" };
                var response = await _requestClient.GetResponse<GetSystemConfigEventResponse>(request, cancellationToken);
                
                if (response.Message.Success && !string.IsNullOrEmpty(response.Message.Response))
                {
                    _tavilyKey = response.Message.Response;
                    return _tavilyKey;
                }
            }
            catch
            {
                // Fallback to empty string if request fails
            }

            _tavilyKey = string.Empty;
            return _tavilyKey;
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
        private static bool IsAvoid(string host, HashSet<string> avoid)
        {
            if (string.IsNullOrEmpty(host)) return false;
            return avoid.Contains(host) || avoid.Any(d => host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }
        private static bool IsTrusted(string host, HashSet<string> trusted)
        {
            if (string.IsNullOrEmpty(host)) return false;
            return trusted.Contains(host) || trusted.Any(d => host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }
        public async Task<List<TavilyItem>> TavilySearchParallelAsync(List<string> queries, HashSet<string> trustedDomains, HashSet<string> avoidDomains)
        {
            var tavilyKey = await GetTavilyKeyAsync();
            var tasks = queries.Select(async q =>
            {
                try
                {
                    var res = await _tavily.SearchAsync(apiKey: tavilyKey, query: q);
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

        public async Task<List<TavilyItem>> TavilySearchParallelExhaustiveAsync(List<string> queries, HashSet<string> trustedDomains, HashSet<string> avoidDomains, int limitPerDomain = 3, int maxTake = 90)
        {
            var tavilyKey = await GetTavilyKeyAsync();
            var tasks = queries.Select(async q =>
            {
                try
                {
                    var res = await _tavily.SearchAsync(apiKey: tavilyKey, query: q);
                    var results = res?.Results;
                    if (results == null || results.Count == 0)
                        return new List<TavilyItem>();

                    var list = new List<TavilyItem>();
                    foreach (var r in results)
                    {
                        var host = DomainOf(r.Url);
                        if (IsAvoid(host, avoidDomains) || LooksLikePromoOrSyllabus(r.Title, r.Url))
                            continue;

                        double score = r.Score;

                        // Boost domain tin cậy (giữ nguyên logic nhẹ)
                        if (IsTrusted(host, trustedDomains)) score += 0.15;
                        if (host.Equals("react.dev", StringComparison.OrdinalIgnoreCase)) score += 0.20;
                        if (host.Equals("redux.js.org", StringComparison.OrdinalIgnoreCase)) score += 0.10;
                        if (host.Equals("developer.mozilla.org", StringComparison.OrdinalIgnoreCase)) score += 0.05;

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
            all.Sort((a, b) => b.Score.CompareTo(a.Score));

            // ❗️ khác biệt với hàm cũ: cho phép limitPerDomain lớn hơn và lấy nhiều hơn
            var dedup = DedupResults(all, limitPerDomain: Math.Max(1, limitPerDomain));
            return dedup.Take(Math.Max(maxTake, 30)).ToList();
        }
    }
}
