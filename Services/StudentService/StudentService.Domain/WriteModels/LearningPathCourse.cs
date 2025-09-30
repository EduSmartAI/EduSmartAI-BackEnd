namespace StudentService.Domain.WriteModels;

public partial class LearningPathCourse
{
    public Guid LearningPathCourseId { get; set; }

    public Guid LearningPathMajorId { get; set; }

    public Guid? InternalCourseId { get; set; }

    public int? Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? StepName { get; set; }

    public string? ExternalCourseLink { get; set; }

    public string? ExternalCourseReason { get; set; }

    public decimal? ExternalCourseRating { get; set; }

    public string? ExternalCourseLevel { get; set; }

    public string? ExternalCourseDuration { get; set; }

    public string? ExternalCourseProvider { get; set; }

    public virtual LearningPathMajor LearningPathMajor { get; set; } = null!;
}
