using BaseService.Common.ApiEntities;
using System.Text.Json.Serialization;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard
{
	public record GetModuleDashboardEventResponse : AbstractApiResponse<ModuleDashboardContract>
	{
		public override ModuleDashboardContract Response { get; set; } = default!;
	}

	public record ModuleDashboardContract
	{
		public Guid StudentId { get; init; }
		public Guid CourseId { get; init; }

		/// <summary>Danh sách module với các chỉ số tổng hợp.</summary>
		public IReadOnlyList<ModuleDashboardItemContract> Modules { get; init; } = Array.Empty<ModuleDashboardItemContract>();

		/// <summary>Chỉ số tổng hợp toàn khóa tính theo các module ở trên.</summary>
		public ModuleDashboardTotalsContract Totals { get; init; } = new();
	}

	/// <summary>
	/// Chỉ số hiển thị cho từng module.
	/// </summary>
	public record ModuleDashboardItemContract
	{
		// Identity / meta
		public Guid ModuleId { get; init; }
		public string ModuleName { get; init; } = default!;
		public int PositionIndex { get; init; }
		public int? Level { get; init; }           // modules.level (0..5), nullable theo DB
		public bool IsCore { get; init; }          // modules.is_core
		public string? Description { get; init; }  // modules.description

		// Progress (từ v_user_module_progress / user_module_progress)
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public ModuleProgressStatus Status { get; init; } = ModuleProgressStatus.NotStarted;

		/// <summary>Tổng số bài học (video) trong module (active).</summary>
		public int LessonsVideoTotal { get; init; }

		/// <summary>Số bài học đã hoàn thành (status = 2).</summary>
		public int LessonsCompleted { get; init; }

		/// <summary>Phần trăm hoàn thành [0..100], làm tròn 2 chữ số thập phân ở tầng repo.</summary>
		public decimal PercentCompleted { get; init; }

		/// <summary>Số bài đang học dở (status = 1). Không bắt buộc nhưng hữu ích cho UI.</summary>
		public int LessonsInProgress { get; init; }

		// Duration (đơn vị phút để đơn giản hóa serialization)
		/// <summary>Thời lượng dự kiến của module (tổng), ưu tiên modules.duration_minutes; fallback SUM(lessons.video_duration_sec)/60.</summary>
		public int ModuleDurationMinutes { get; init; }

		/// <summary>Tổng thời gian học thực tế của học viên (SUM user_lesson_progress.duration_watched_sec)/60.</summary>
		public int ActualStudyMinutes { get; init; }

		// Quiz counts (không bao gồm điểm số — sẽ lấy từ QuizService khi cần)
		/// <summary>Số quiz gắn ở cấp module (module_quizzes).</summary>
		public int ModuleQuizCount { get; init; }

		/// <summary>Số quiz gắn ở cấp bài học (lesson_quizzes trong phạm vi module).</summary>
		public int LessonQuizCount { get; init; }

		/// <summary>Tổng số quiz của module (module-level + lesson-level).</summary>
		public int TotalQuizCount { get; init; }

		// Quiz metrics (để nullable vì thuộc QuizService, chưa bắt buộc)
		/// <summary>Điểm trung bình quiz của module (0..10, 0..100, tùy hệ quy chiếu của bạn) — để nullable, bạn điền khi có.</summary>
		public decimal? AverageQuizScore { get; init; }

		// AI evaluation (để nullable — thuộc AIService, có thể tích hợp sau)
		/// <summary>Điểm AI đánh giá (0..100).</summary>
		public int? AiScore { get; init; }

		/// <summary>Tóm tắt AI feedback.</summary>
		public string? AiFeedbackSummary { get; init; }

		/// <summary>Điểm mạnh do AI tổng hợp.</summary>
		public IReadOnlyList<string>? AiStrengths { get; init; }

		/// <summary>Điểm cần cải thiện do AI tổng hợp.</summary>
		public IReadOnlyList<string>? AiImprovements { get; init; }

		// Timestamps (nếu StudentService muốn hiển thị)
		public DateTime? StartedAtUtc { get; init; }
		public DateTime? CompletedAtUtc { get; init; }
		public DateTime? UpdatedAtUtc { get; init; }
	}

	/// <summary>
	/// Chỉ số tổng hợp toàn khóa (tính từ các module đã trả về).
	/// </summary>
	public record ModuleDashboardTotalsContract
	{
		public int ModulesCount { get; init; }

		public int LessonsTotal { get; init; }
		public int LessonsCompleted { get; init; }
		public decimal PercentCompleted { get; init; }   // (LessonsCompleted / LessonsTotal) * 100 (nếu LessonsTotal>0)

		public int TotalModuleDurationMinutes { get; init; } // SUM ModuleDurationMinutes
		public int TotalActualStudyMinutes { get; init; }    // SUM ActualStudyMinutes

		public int TotalModuleQuizCount { get; init; }   // SUM ModuleQuizCount
		public int TotalLessonQuizCount { get; init; }   // SUM LessonQuizCount
		public int TotalQuizCount { get; init; }         // SUM TotalQuizCount

		// Chỗ trống cho tổng hợp điểm/AI (nếu cần sau này)
		public decimal? AverageQuizScore { get; init; }  // trung bình có trọng số (tùy bạn định nghĩa)
		public int? AverageAiScore { get; init; }        // trung bình (tùy bạn định nghĩa)
	}
}
