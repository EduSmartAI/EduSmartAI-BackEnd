using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard
{
	public sealed record GetLessonDashboardEventResponse : AbstractApiResponse<LessonDashboardContract>
	{
		public override LessonDashboardContract Response { get; set; } = default!;
	}

	public sealed class LessonDashboardContract
	{
		public Guid StudentId { get; init; }
		public Guid CourseId { get; init; }
		public IReadOnlyList<LessonDashboardModuleGroup> Modules { get; init; } = Array.Empty<LessonDashboardModuleGroup>();
		public LessonDashboardTotals Totals { get; init; } = new();
	}

	public sealed class LessonDashboardModuleGroup
	{
		public Guid ModuleId { get; init; }
		public string ModuleName { get; init; } = default!;
		public int PositionIndex { get; init; }
		public IReadOnlyList<LessonDashboardItem> Lessons { get; init; } = Array.Empty<LessonDashboardItem>();
	}

	// Dùng class + init: enrich bằng “with” nếu đổi sang record; nếu giữ class, enrich bằng tạo object mới.
	public sealed class LessonDashboardItem
	{
		// identity/meta (từ CourseService)
		public Guid LessonId { get; init; }
		public string Title { get; init; } = default!;
		public int PositionIndex { get; init; }
		public bool IsActive { get; init; }
		public string? VideoUrl { get; init; }

		// progress
		public LessonStatus Status { get; init; }
		public int? CurrentSecond { get; init; }
		public int VideoDurationSeconds { get; init; }
		public int ActualStudyMinutes { get; init; }
		public decimal PercentWatched { get; init; }

		// quiz (lesson) từ CourseService/QuizService
		public int LessonQuizCount { get; init; }
		public decimal? AverageQuizScore { get; init; }   // latest score_100

		// AI (scope = Lesson) – enrich tại StudentService
		public int? AiScore { get; init; }
		public int? AiScoreRaw { get; init; }
		public string? AiFeedbackSummary { get; init; }
		public IReadOnlyList<string>? AiStrengths { get; init; }
		public IReadOnlyList<AiImprovementDto>? AiImprovementResources { get; init; }

		// timestamps
		public DateTime? StartedAtUtc { get; init; }
		public DateTime? CompletedAtUtc { get; init; }
		public DateTime? UpdatedAtUtc { get; init; }
		public DateTime? AiEvaluatedAtUtc { get; init; }
	}

	public sealed class LessonDashboardTotals
	{
		public int ModulesCount { get; init; }
		public int LessonsCount { get; init; }
		public int TotalVideoDurationMinutes { get; init; }
		public int TotalActualStudyMinutes { get; init; }
		public int TotalLessonQuizCount { get; init; }
		public decimal? AverageQuizScore { get; init; }
		public int? AverageAiScore { get; init; }
	}
}
