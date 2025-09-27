namespace StudentService.Domain.ReadModels;

public class LearningPathCourseCollection
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
    public static LearningPathCourseCollection FromWriteModel(WriteModels.LearningPathCourse model)
    {
        return new LearningPathCourseCollection
        {
            LearningPathCourseId = model.LearningPathCourseId,
            LearningPathMajorId = model.LearningPathMajorId,
            InternalCourseId = model.InternalCourseId,
            Position = model.Position,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            StepName = model.StepName
        };
    }

}