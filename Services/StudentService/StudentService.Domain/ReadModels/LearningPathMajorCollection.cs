using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class LearningPathMajorCollection
{
    public Guid LearningPathMajorId { get; set; }

    public Guid PathId { get; set; }

    public Guid MajorId { get; set; }

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<LearningPathCourseCollection> LearningPathCourses { get; set; } = new List<LearningPathCourseCollection>();

    public virtual ICollection<LearningPathMajorTypeCollection> LearningPathMajorTypes { get; set; } = new List<LearningPathMajorTypeCollection>();

    public static LearningPathMajorCollection FromWriteModel(LearningPathMajor model)
    {
        return new LearningPathMajorCollection
        {
            LearningPathMajorId = model.LearningPathMajorId,
            PathId = model.PathId,
            MajorId = model.MajorId,
            Reason = model.Reason,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            LearningPathCourses = (model.LearningPathCourses ?? Enumerable.Empty<LearningPathCourse>())
                    .Select(LearningPathCourseCollection.FromWriteModel)
                    .ToList(),
            LearningPathMajorTypes = (model.LearningPathMajorTypes ?? Enumerable.Empty<LearningPathMajorType>())
                    .Select(LearningPathMajorTypeCollection.FromWriteModel)
                    .ToList()
        };
    }

}