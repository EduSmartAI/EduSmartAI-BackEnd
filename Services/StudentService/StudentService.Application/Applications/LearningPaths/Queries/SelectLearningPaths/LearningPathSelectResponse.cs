using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths
{
    public record LearningPathSelectResponse : AbstractApiResponse<LearningPathSelectDto>
    {
        public override LearningPathSelectDto Response { get; set; } = new();
    }

    public record LearningPathSelectDto
    {
        public int Status { get; set; }
        public string PathName { get; set; } = string.Empty;
        public decimal CompletionPercent { get; set; }
        public string? SummaryFeedback { get; set; }
        public string? HabitAndInterestAnalysis { get; set; }
        public string? Personality { get; set; }
        public string? LearningAbility { get; set; }
        public List<PraticalAbilityFeedback> praticalAbilityFeedbacks { get; set; } = [];
        public BasicLearningPathDto BasicLearningPath { get; set; } = new();
        public List<InternalLearningPathDto> InternalLearningPath { get; set; } = [];
        public List<ExternalLearningPathDto> ExternalLearningPath { get; set; } = [];
    }

    #region Basic
    public record BasicLearningPathDto
    {
        // CHANGED: bỏ Courses, thay bằng CourseGroups
        public List<CourseGroupDto> CourseGroups { get; set; } = [];
    }
    #endregion

    #region Internal
    public record InternalLearningPathDto
    {
        public string? MajorId { get; set; }
        public string? MajorCode { get; set; }
        public string? Reason { get; set; }
        public int? PositionIndex { get; set; }

        // CHANGED: bỏ MajorCourse, thay bằng MajorCourseGroups
        public List<CourseGroupDto> MajorCourseGroups { get; set; } = [];
    }
    #endregion

    #region External
    public record ExternalLearningPathDto
    {
        public string? MajorId { get; set; }
        public string? MajorCode { get; set; }
        public string? Reason { get; set; }
        public List<ExternalStepDto> Steps { get; set; } = [];
    }

    public record ExternalStepDto
    {
        public string? Title { get; set; }
        public int Duration_Weeks { get; set; }
        public List<SuggestedCourseDto> Suggested_Courses { get; set; } = [];
    }

    public record SuggestedCourseDto
    {
        public string? Title { get; set; }
        public string? Link { get; set; }
        public string? Provider { get; set; }
        public string? Reason { get; set; }
        public string? Level { get; set; }
        public string? Rating { get; set; }
        public int? Est_Duration_Weeks { get; set; }
    }
    #endregion

    #region Shared course model
    public record CourseItemDto
    {
        public string? CourseId { get; set; }
        public int SemesterPosition { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public short Status { get; set; }
        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public string? CourseImageUrl { get; set; }
        public int LearnerCount { get; set; }
        public int DurationMinutes { get; set; }
        public int DurationHours { get; set; }
        public int Level { get; set; }
        public decimal Price { get; set; }
        public decimal DealPrice { get; set; }
        public bool isEnrolled { get; set; }
        public bool isWishList { get; set; }
        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public List<string> TagNames { get; set; } = [];
    }

    // NEW: nhóm theo môn
    public record CourseGroupDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public short Status { get; set; }
        public int SemesterPosition { get; set; }
        public string? AnalysisMarkdown { get; set; }
        public List<CourseItemDto> Courses { get; set; } = [];
    }
    #endregion
    #region Ability
    public class PraticalAbilityFeedback
    {
        public string AnalysisMarkDown { get; set; } = string.Empty;
    }
    #endregion
}
