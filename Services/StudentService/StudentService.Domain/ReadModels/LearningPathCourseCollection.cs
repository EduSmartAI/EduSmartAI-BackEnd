namespace StudentService.Domain.ReadModels;

public class LearningPathCourseCollection
{
    public Guid LearningPathCourseId { get; set; }

    public Guid LearningPathMajorId { get; set; }

    public Guid? InternalCourseId { get; set; }

    public int? Position { get; set; }
    
    public short Status { get; set; }
    
    public string? SubjectCode { get; set; }

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
    public static LearningPathCourseCollection FromWriteModel(WriteModels.LearningPathCourse model)
    {
        return new LearningPathCourseCollection
        {
            LearningPathCourseId = model.LearningPathCourseId,
            LearningPathMajorId = model.LearningPathMajorId,
            InternalCourseId = model.InternalCourseId,
            Position = model.Position,
            Status = model.Status,
            // SubjectCode = model.SubjectCode,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            StepName = model.StepName,
            ExternalCourseLink = model.ExternalCourseLink,
            ExternalCourseReason = model.ExternalCourseReason,
            ExternalCourseRating = model.ExternalCourseRating,
            ExternalCourseLevel = model.ExternalCourseLevel,
            ExternalCourseDuration = model.ExternalCourseDuration,
            ExternalCourseProvider = model.ExternalCourseProvider
        };
    }

}