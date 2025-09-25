using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class LearningPathMajorTypeCollection
{
    public Guid TypeId { get; set; }

    public Guid LearningPathMajorId { get; set; }

    public string TypeName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public static LearningPathMajorTypeCollection FromWriteModel(LearningPathMajorType model)
    {
        return new LearningPathMajorTypeCollection
        {
            TypeId = model.TypeId,
            LearningPathMajorId = model.LearningPathMajorId,
            TypeName = model.TypeName,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }

}