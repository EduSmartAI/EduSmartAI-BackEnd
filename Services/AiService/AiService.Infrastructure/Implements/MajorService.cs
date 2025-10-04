using AiService.Application.Contracts;
using AiService.Application.Interfaces;
using AiService.Domain.Models;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Implements
{
    public class MajorService : IMajorService
    {
        private readonly ICommandRepository<MajorEmbedding> _majorEmbeddingRepository;
        public MajorService(ICommandRepository<MajorEmbedding> majorEmbeddingRepository)
        {
            _majorEmbeddingRepository = majorEmbeddingRepository;
        }
        public async Task<List<AiRecommendContracts.Major>> LoadMajorsAsync(CancellationToken ct)
        {
            var query = _majorEmbeddingRepository.Find(
                predicate: null,
                isTracking: false,
                cancellationToken: ct
            );

            // Vì Find trả về IQueryable<MajorEmbedding?> nên lọc null trước khi select
            var all = await query
                .Where(x => x != null)
                .Select(x => new
                {
                    x!.MajorCode,
                    x.MajorName,
                    x.Content
                })
                .ToListAsync(ct);

            // Group theo MajorCode và chọn bản có Content dài nhất
            var result = all
                .GroupBy(x => x.MajorCode)
                .Select(g =>
                {
                    var best = g
                        .OrderByDescending(x => (x.Content ?? string.Empty).Length)
                        .First();

                    return new AiRecommendContracts.Major(
                        best.MajorCode,
                        best.MajorName,
                        best.Content ?? string.Empty
                    );
                })
                .ToList();

            return result;
        }
    }
}
