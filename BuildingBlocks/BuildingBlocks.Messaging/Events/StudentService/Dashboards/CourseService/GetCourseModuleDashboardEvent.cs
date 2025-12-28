using BaseService.Common.ApiEntities;
using System.Text.Json.Serialization;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService
{
	public sealed record GetCourseModuleDashboardEventResponse : AbstractApiResponse<CourseModuleDashboardContract>
	{
		public override CourseModuleDashboardContract Response { get; set; } = default!;
	}

	public sealed class CourseModuleDashboardContract
	{
		public Guid StudentId { get; init; }
		public Guid CourseId { get; init; }
		public IReadOnlyList<CourseModuleDashboardItem> Modules { get; init; } = Array.Empty<CourseModuleDashboardItem>();
		public CourseModuleDashboardTotals Totals { get; init; } = new();
	}

	// 1 module
	public sealed class CourseModuleDashboardItem
	{
		// meta
		public Guid ModuleId { get; init; }
		public string ModuleName { get; init; } = default!;
		public int PositionIndex { get; init; }
		public int? Level { get; init; }
		public bool IsCore { get; init; }
		public string? Description { get; init; }

		// progress
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public ModuleProgressStatus Status { get; init; }  // 0/1/2
		public int LessonsVideoTotal { get; init; }
		public int LessonsCompleted { get; init; }
		public decimal PercentCompleted { get; init; }
		public int LessonsInProgress { get; init; }

		// durations (phút)
		public int ModuleDurationMinutes { get; init; }     // modules.duration_minutes || SUM(lessons.video_duration_sec)/60
		public int ActualStudyMinutes { get; init; }        // SUM(user_lesson_progress.duration_watched_sec)/60

		// quiz
		public int ModuleQuizCount { get; init; }
		public int LessonQuizCount { get; init; }
		public int TotalQuizCount { get; init; }
		public decimal? AverageQuizScore { get; set; }

		// timestamps
		public DateTime? StartedAtUtc { get; init; }
		public DateTime? CompletedAtUtc { get; init; }
		public DateTime UpdatedAtUtc { get; init; }
	}

	public sealed class CourseModuleDashboardTotals
	{
		public int ModulesCount { get; init; }
		public int LessonsTotal { get; init; }
		public int LessonsCompleted { get; init; }
		public decimal PercentCompleted { get; init; }   // guard nếu LessonsTotal=0
		public int TotalModuleDurationMinutes { get; init; }
		public int TotalActualStudyMinutes { get; init; }
		public int TotalModuleQuizCount { get; init; }
		public int TotalLessonQuizCount { get; init; }
		public int TotalQuizCount { get; init; }
	}
}
