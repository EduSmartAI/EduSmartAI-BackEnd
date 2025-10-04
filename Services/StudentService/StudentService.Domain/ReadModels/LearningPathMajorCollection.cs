namespace StudentService.Domain.ReadModels;

public class LearningPathMajorCollection
{
    public Guid LearningPathMajorId { get; set; }
    public Guid PathId { get; set; }
    public string MajorCode { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }

    /// <summary>1: Internal, 2: External</summary>
    public short Type { get; set; }

    public virtual ICollection<LearningPathCourseCollection> LearningPathCourses { get; set; }
        = new List<LearningPathCourseCollection>();

    public static LearningPathMajorCollection FromWriteModel(WriteModels.LearningPathMajor model)
    {
        return new LearningPathMajorCollection
        {
            LearningPathMajorId = model.LearningPathMajorId,
            PathId = model.PathId,
            MajorCode = model.MajorCode,
            Reason = model.Reason,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            Type = model.Type,
            LearningPathCourses = (model.LearningPathCourses ?? new List<WriteModels.LearningPathCourse>())
                .Select(LearningPathCourseCollection.FromWriteModel)
                .ToList()
        };
    }

}