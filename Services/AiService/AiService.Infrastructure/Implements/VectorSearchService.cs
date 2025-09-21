using AiService.Application.Interfaces;
using AiService.Domain.Models;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Infrastructure.Implements
{
    public class VectorSearchService : IVectorSearchService
    {
        ICommandRepository<MajorEmbedding> _majorEmbeddingRepository;
        public VectorSearchService(ICommandRepository<MajorEmbedding> majorEmbeddingRepository)
        {
            _majorEmbeddingRepository = majorEmbeddingRepository;
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
    }
}
