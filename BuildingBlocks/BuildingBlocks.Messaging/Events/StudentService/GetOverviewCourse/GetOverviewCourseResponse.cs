using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse
{
    public record GetOverviewCourseResponse : AbstractApiResponse<OverviewCourseInfoDto>
    {
        public override OverviewCourseInfoDto Response { get; set; } = new OverviewCourseInfoDto();
    }
    public record class OverviewCourseInfoDto
    {
        public Guid? UserCourseProgressId { get; set; }

        public Guid? UserId { get; set; }

        public Guid? CourseId { get; set; }

        public int? LessonsTotal { get; set; }

        public int? LessonsCompleted { get; set; }

        public decimal? PercentCompleted { get; set; }

        public short? Status { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public short? Level { get; set; }

        public int? DurationMinutes { get; set; }

        public decimal? DurationHours { get; set; }

        public Guid? TeacherId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? LearnerCount { get; set; }

        public long? TotalDurationWatchedSec { get; set; }

        public long? TotalModuleQuizzes { get; set; }

        public long? TotalLessonQuizzes { get; set; }

        public string LessonProgressList { get; set; } = string.Empty;
    }
}
