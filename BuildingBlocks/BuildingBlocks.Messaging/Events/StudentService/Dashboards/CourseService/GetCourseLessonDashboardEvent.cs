using BaseService.Common.ApiEntities;
using System.Text.Json.Serialization;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService
{
	public sealed record GetCourseLessonDashboardEvent(Guid StudentId, Guid CourseId);

	public sealed record GetCourseLessonDashboardEventResponse : AbstractApiResponse<CourseLessonDashboardContract>
	{
		public override CourseLessonDashboardContract Response { get; set; } = default!;
	}

	public sealed class CourseLessonDashboardContract
	{
		public Guid StudentId { get; init; }
		public Guid CourseId { get; init; }
		public IReadOnlyList<CourseLessonModuleGroup> Modules { get; init; } = Array.Empty<CourseLessonModuleGroup>();
		public CourseLessonTotals Totals { get; init; } = new();
	}

	public sealed class CourseLessonModuleGroup
	{
		public Guid ModuleId { get; init; }
		public string ModuleName { get; init; } = default!;
		public int PositionIndex { get; init; }
		public IReadOnlyList<CourseLessonItem> Lessons { get; init; } = Array.Empty<CourseLessonItem>();
	}

	public sealed class CourseLessonItem
	{
		// identity/meta
		public Guid LessonId { get; init; }
		public string Title { get; init; } = default!;
		public int PositionIndex { get; init; }
		public bool IsActive { get; init; }
		public string? VideoUrl { get; init; }

		// progress
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public LessonStatus Status { get; init; }
		public int? CurrentSecond { get; init; }
		public int VideoDurationSeconds { get; init; }
		public int ActualStudyMinutes { get; init; }
		public decimal PercentWatched { get; init; }

		// quiz (lesson)
		public int LessonQuizCount { get; init; }
		public decimal? AverageQuizScore { get; init; } // latest score_100 (0..100), nullable

		// timestamps
		public DateTime? StartedAtUtc { get; init; }
		public DateTime? CompletedAtUtc { get; init; }
		public DateTime? UpdatedAtUtc { get; init; }
	}

	public sealed class CourseLessonTotals
	{
		public int ModulesCount { get; init; }
		public int LessonsCount { get; init; }
		public int TotalVideoDurationMinutes { get; init; }
		public int TotalActualStudyMinutes { get; init; }
		public int TotalLessonQuizCount { get; init; }
		public decimal? AverageQuizScore { get; init; }
	}
}
