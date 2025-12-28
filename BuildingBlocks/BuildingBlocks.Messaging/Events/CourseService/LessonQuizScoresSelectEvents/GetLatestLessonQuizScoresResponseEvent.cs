using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents
{
	public sealed record GetLatestLessonQuizScoresResponseEvent : AbstractApiResponse<GetLatestLessonQuizScoresPayload>
	{
		public override GetLatestLessonQuizScoresPayload Response { get; set; } = default!;
	}

	public sealed class GetLatestLessonQuizScoresPayload
	{
		public IReadOnlyList<LessonLatestQuizScore> Lessons { get; init; } = Array.Empty<LessonLatestQuizScore>();
	}

	public sealed class LessonLatestQuizScore
	{
		public Guid LessonId { get; init; }
		public int? LatestScore100 { get; init; }          // null nếu chưa có attempt
		public int AttemptCount { get; init; }             // tổng số attempt (tạm thời để bạn giám sát dữ liệu)
	}
}
