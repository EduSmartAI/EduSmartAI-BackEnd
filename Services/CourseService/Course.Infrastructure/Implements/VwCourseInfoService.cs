using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;

namespace Course.Infrastructure.Implements
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="_courseRepository"></param>
    /// <param name="_unitOfWork"></param>
    public class VwCourseInfoService(
        ICommandRepository<VwCourseInfo> _courseRepository,
        IUnitOfWork _unitOfWork
        ) : IVwCourseInforService
    {
        /// <summary>
        /// Get lesson infor for AI to use
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<GetLessonInfoResponse> GetAllInforLessonById(GetLessonInfoEvent request, CancellationToken ct = default)
        {
            if (request.LessonId == Guid.Empty)
            {
                return new GetLessonInfoResponse
                {
                    Response = new LessonInforData(),
                    Success = false
                };
            }

            var userPart = request.StudentId;
            var cacheKey = $"lessoninfo:{request.LessonId:N}:u:{userPart}";

            // 1) Try cache trước
            var cached = await _unitOfWork.CacheGetAsync<LessonInforData>(cacheKey);
            if (cached is not null)
            {
                return new GetLessonInfoResponse
                {
                    Response = cached,
                    Success = true
                };
            }

            // 2) Miss cache -> query view, project thẳng ra DTO
            var dto = await _courseRepository
                .Find(x => x.LessonId == request.LessonId, isTracking: false, ct)
                .Select(x => new LessonInforData
                {
                    TranscriptId = x!.TranscriptId,
                    TranscriptLanguage = x.TranscriptLanguage ?? string.Empty,
                    TranscriptText = x.TranscriptText ?? string.Empty,
                    LessonTitle = x.LessonTitle ?? string.Empty,
                    VideoDurationSec = x.VideoDurationSec
                })
                .FirstOrDefaultAsync(ct);

            var result = dto ?? new LessonInforData();

            // 3) Lưu cache (TTL 30 phút, bạn đổi tuỳ ý)
            await _unitOfWork.CacheSetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

            return new GetLessonInfoResponse
            {
                Response = result,
                Success = true
            };
        }
    }
}
