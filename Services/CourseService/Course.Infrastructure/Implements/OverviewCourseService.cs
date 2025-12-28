using BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse;

namespace Course.Infrastructure.Implements
{
    public class OverviewCourseService(
        ICommandRepository<VwOverviewCourseProgress> _courseRepository,
        IUnitOfWork _unitOfWork
        ) : IOverviewCourseService
    {
        public async Task<GetOverviewCourseResponse> GetOverviewCourseByIds(GetOverviewCourseEvents events, CancellationToken ct = default)
        {
            if (events.courseId == Guid.Empty)
                return new GetOverviewCourseResponse { Success = false };

            var userPart = events.StudentId;
            //var cacheKey = $"coursehier:{events.courseId:N}:u:{userPart}";

            //var cached = await _unitOfWork.CacheGetAsync<GetOverviewCourseResponse>(cacheKey);
            //if (cached is not null)
            //    return cached;

            var row = await _courseRepository.FirstOrDefaultAsync(x => x.CourseId == events.courseId && x.UserId == userPart, ct);

            var result = new GetOverviewCourseResponse
            {
                Success = row is not null
            };

            // 3. Map vào grouped DTO
            if (row is not null)
            {
                var overview = new OverviewCourseInfoDto
                {
                    UserCourseProgressId = row.UserCourseProgressId,
                    UserId = row.UserId,
                    CourseId = row.CourseId,
                    LessonsTotal = row.LessonsTotal,
                    LessonsCompleted = row.LessonsCompleted,
                    PercentCompleted = row.PercentCompleted,
                    Status = row.Status,
                    StartedAt = row.StartedAt,
                    CompletedAt = row.CompletedAt,
                    CreatedAt = row.CreatedAt,
                    UpdatedAt = row.UpdatedAt,
                    Level = row.Level,
                    DurationMinutes = row.DurationMinutes,
                    DurationHours = row.DurationHours,
                    TeacherId = row.TeacherId,
                    Title = row.Title ?? string.Empty,
                    LearnerCount = row.LearnerCount,
                    TotalDurationWatchedSec = row.TotalDurationWatchedSec,
                    TotalModuleQuizzes = row.TotalModuleQuizzes,
                    TotalLessonQuizzes = row.TotalLessonQuizzes,
                    LessonProgressList = row.LessonProgressList ?? string.Empty
                };

                // Giả sử GetInfoEvaluationGroupedDto có property OverviewCourse
                result.Response = overview;
            }

            // 4. Cache kết quả
            //await _unitOfWork.CacheSetAsync(cacheKey, result, TimeSpan.FromMinutes(1));

            return result;
        }
        /// <summary>
        /// Get course stats about performance of learner
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="studentId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<CoursePaceStatsDto?> GetCoursePaceStatsAsync(Guid courseId, Guid studentId, CancellationToken ct = default)
        {
            if (courseId == Guid.Empty)
                return null;

            var list = await _courseRepository.Find(
                    x => x.CourseId == courseId
                         && x.LessonsCompleted.HasValue && x.LessonsCompleted > 0
                         && x.TotalDurationWatchedSec.HasValue && x.TotalDurationWatchedSec > 0,
                    isTracking: false,
                    cancellationToken: ct
                )
                .ToListAsync(ct);

            if (list.Count == 0)
                return null;

            var perUser = list
                .Select(x => new
                {
                    x!.UserId,
                    MinutesPerLesson =
                        (double)x!.TotalDurationWatchedSec!.Value / 60.0 /
                        x.LessonsCompleted!.Value
                })
                .ToList();

            // dùng LearnerCount từ view, fallback sang số người có progress
            var totalLearners = list.FirstOrDefault()?.LearnerCount ?? perUser.Count;

            var user = perUser.FirstOrDefault(x => x.UserId == studentId);
            if (user is null)
            {
                return new CoursePaceStatsDto
                {
                    AverageMinutesPerLesson = 0,
                    LearnerCount = totalLearners,
                    Rank = 0,
                    FasterCount = 0,
                    SlowerCount = 0,
                    FasterPercent = 0
                };
            }

            //var avgMinutesPerLesson = perUser.Average(x => x.MinutesPerLesson); // Thêm sau


            var slowerCount = perUser.Count(x => x.MinutesPerLesson < user.MinutesPerLesson);
            var fasterCount = perUser.Count(x => x.MinutesPerLesson > user.MinutesPerLesson);
            var rank = slowerCount + 1;
            var othersCount = totalLearners - 1;
            var fasterPercent = (othersCount > 0 && fasterCount > 0)
                ? fasterCount * 100.0 / othersCount
                : 0;

            return new CoursePaceStatsDto
            {
                AverageMinutesPerLesson = user.MinutesPerLesson,
                LearnerCount = totalLearners,
                Rank = rank,
                FasterCount = fasterCount,
                SlowerCount = slowerCount,
                FasterPercent = fasterPercent
            };
        }

    }
}
