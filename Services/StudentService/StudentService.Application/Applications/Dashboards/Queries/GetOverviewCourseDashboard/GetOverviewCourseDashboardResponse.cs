using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Dashboards.Queries.GetOverviewCourseDashboard
{
    public sealed record GetOverviewCourseDashboardResponse : AbstractApiResponse<OverviewCourseContract>
    {
        public override OverviewCourseContract Response { get; set; } = new OverviewCourseContract();
    }
    public sealed class OverviewCourseContract
    {
        public string CourseName { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
        public int TotalVideos { get; set; }
        public int TotalQuizzes { get; set; }
        public DateTime StartDate { get; set; }
        public int Level { get; set; }
        public ProgressSection Progress { get; set; } = new();
        public string AiEvaluationMarkdown { get; set; } = string.Empty;
        public PerformanceSection Performance { get; set; } = new();
        public LearningBehaviorSection LearningBehavior { get; set; } = new();
		public List<SuggestedCourseDetailsDto> SuggestedCourses { get; set; } = new();
	}
    public sealed class ProgressSection
    {
        public double CompletedPercent { get; set; }        // 68%
        public int LessonsCompleted { get; set; }              // 106
        public int LessonsTotal { get; set; }                // 156
        public int QuizTotal { get; set; }                  // 12
        public double AverageScore { get; set; }            // 7.8/10
        public double AverageAiScore { get; set; }            // 7.8/10
        public TimeSpan TotalLearningTime { get; set; }     // 18 giờ 45 phút
    }
    public sealed class PerformanceSection
    {
        public double AvgMinutesPerLesson { get; set; }
        public int Rank { get; set; }
        public int FasterCount { get; set; }
        public int SlowerCount { get; set; }
        public string Analysis { get; set; } = string.Empty;
    }
    public sealed class LearningBehaviorSection
    {
        public DateTime LastAccessed { get; set; }
        public short MostActiveSlot { get; set; }
        public long? TotalPauseCount { get; set; }
        public long? ScrollVideoCount { get; set; }
        public int RewindTimes { get; set; }
        public double AverageRewatchPerLesson { get; set; }
        public double AveragePausePerLesson { get; set; }
        public List<LearningStreakItem> Streaks { get; set; } = [];
    }
    public sealed class LearningStreakItem
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
    }
    public sealed class OverviewAiEvaluationResult
    {
        public string Summary { get; set; } = string.Empty;
        public double AverageScore100Raw { get; set; }
        public double AverageScore100 { get; set; }
    }
    public sealed class CoursePaceStatsDto
    {
        public double AverageMinutesPerLesson { get; set; }
        public int LearnerCount { get; set; }
    }

	public sealed class SuggestedCourseDetailsDto
	{
		public Guid CourseId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string ShortDescription { get; set; } = string.Empty;
		public string CourseImageUrl { get; set; } = string.Empty;
		public int Level { get; set; }
		public decimal Price { get; set; }
		public decimal? DealPrice { get; set; }
		public Guid TeacherId { get; set; }
		public string? TeacherName { get; set; }
		public string SubjectCode { get; set; } = string.Empty;
	}

}
