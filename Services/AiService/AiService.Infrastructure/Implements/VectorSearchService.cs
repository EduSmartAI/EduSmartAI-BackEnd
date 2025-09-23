using AiService.Application.Interfaces;
using AiService.Domain.Models;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using OpenAI.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Infrastructure.Implements
{
    public class VectorSearchService : IVectorSearchService
    {
        ICommandRepository<MajorEmbedding> _majorEmbeddingRepository;
        ICommandRepository<CourseEmbedding> _courseEmbeddingRepository;
        private readonly EmbeddingClient _embed;
        public VectorSearchService(ICommandRepository<MajorEmbedding> majorEmbeddingRepository, ICommandRepository<CourseEmbedding> courseEmbeddingRepository)
        {
            _majorEmbeddingRepository = majorEmbeddingRepository;
            _courseEmbeddingRepository = courseEmbeddingRepository;
        }
        public async Task<List<SearchHit>> SearchTopKAsync(float[] queryEmbedding, int k, CancellationToken ct)
        {
            var qvec = new Vector(queryEmbedding);
            var query = _majorEmbeddingRepository
                .Find(predicate: null, isTracking: false, cancellationToken: ct);

            var rows = await query
                .Where(x => x != null)
                .Select(x => x!)
                .OrderBy(x => x.Embedding!.CosineDistance(qvec))
                .Select(x => new
                {
                    x.MajorCode,
                    x.MajorName,
                    x.Content,
                    Distance = x.Embedding!.CosineDistance(qvec)
                })
                .Take(k)
                .ToListAsync(ct);

            return rows.Select(r => new SearchHit(
                r.MajorCode,
                r.MajorName,
                (r.Content ?? string.Empty) is var c && c.Length > 500 ? c[..500] : (r.Content ?? string.Empty),
                Similarity: (float)(1.0 - r.Distance)
            ))
            .ToList();
        }

        public async Task<IReadOnlyList<DocumentDto>> SearchCoursesTopKAsync(
        float[] queryEmbedding,
        string needle,
        int k,
        CancellationToken ct)
        {
            if (queryEmbedding is null || queryEmbedding.Length == 0) return Array.Empty<DocumentDto>();
            if (k <= 0) k = 80;

            var qvec = new Vector(queryEmbedding);

            const int DEFAULT_PREFETCH = 2000;
            int prefetch = Math.Max(DEFAULT_PREFETCH, k);

            var baseQuery = _courseEmbeddingRepository.Find(
                predicate: x => x != null && x!.Embedding != null,
                isTracking: false,
                cancellationToken: ct
            );

            var candidates = await baseQuery
                .Select(x => new
                {
                    x!.DocId,
                    x.Content,
                    x.Metadata,                           // string? (jsonb)
                    Dist = x.Embedding!.CosineDistance(qvec)
                })
                .OrderBy(x => x.Dist)                     // distance ↑
                .Take(prefetch)
                .ToListAsync(ct);

            needle = (needle ?? string.Empty).Trim();
            var titleKeys = BuildTitleKeys(needle);       // mở rộng từ khóa tiêu đề cho các case như "Cloud Architect"

            static bool TitleContains(string? metadataJson, IReadOnlyList<string> keys)
            {
                if (string.IsNullOrWhiteSpace(metadataJson) || keys.Count == 0) return false;
                try
                {
                    using var doc = JsonDocument.Parse(metadataJson);
                    if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
                    if (!doc.RootElement.TryGetProperty("title", out var t)) return false;
                    var title = (t.GetString() ?? string.Empty).ToLowerInvariant();
                    foreach (var key in keys)
                        if (!string.IsNullOrWhiteSpace(key) && title.Contains(key)) return true;
                    return false;
                }
                catch { return false; }
            }

            static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

            static JsonElement ToJsonElementOrEmpty(string? json)
            {
                var payload = string.IsNullOrWhiteSpace(json) ? "{}" : json!;
                using var doc = JsonDocument.Parse(payload);
                return doc.RootElement.Clone();
            }

            var ranked = candidates
                .Select(r => new
                {
                    r.DocId,
                    Content = r.Content ?? string.Empty,
                    r.Metadata,
                    Score = Clamp01(1.0 - r.Dist),
                    TitleHit = TitleContains(r.Metadata, titleKeys)
                })
                .OrderByDescending(x => x.TitleHit)       // giống Python: title_hit trước
                .ThenByDescending(x => x.Score)
                .Take(k)
                .Select(x => new DocumentDto
                {
                    DocId = x.DocId ?? string.Empty,
                    Content = x.Content,
                    Metadata = ToJsonElementOrEmpty(x.Metadata),
                    Score = x.Score
                })
                .ToList();

            return ranked;
        }
        private static IReadOnlyList<string> BuildTitleKeys(string raw)
        {
            var q = (raw ?? string.Empty).ToLowerInvariant();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(params string[] arr) { foreach (var s in arr) if (!string.IsNullOrWhiteSpace(s)) keys.Add(s.Trim()); }

            // tách token đơn giản
            foreach (var t in q.Split(new[] { ' ', ',', '.', '/', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                Add(t);

            if (q.Contains("cloud architect") || q.Contains("solutions architect") || q.Contains("architect"))
                Add("cloud architect", "solutions architect", "architecting", "architect", "aws", "gcp", "azure",
                    "associate", "professional");

            if (q.Contains("kubernetes") || q.Contains("k8s"))
                Add("kubernetes", "k8s", "cka", "cks", "ckad");

            if (q.Contains("aws")) Add("aws", "architecting on aws", "aws solutions architect");
            if (q.Contains("gcp") || q.Contains("google cloud"))
                Add("gcp", "google cloud", "google cloud architect", "pca");
            if (q.Contains("azure"))
                Add("azure", "azure solutions architect");

            return keys.ToList();
        }
    }
}
