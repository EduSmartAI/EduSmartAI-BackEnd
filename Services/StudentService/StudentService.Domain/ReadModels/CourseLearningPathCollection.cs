using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class CourseLearningPathCollection
{
    public Guid CourseId { get; set; }

    public Guid PathId { get; set; }

    public int? Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public static CourseLearningPathCollection FromWriteModel(CourseLearningPath model)
    {
        return new CourseLearningPathCollection
        {
            CourseId = model.CourseId,
            PathId = model.PathId,
            Position = model.Position,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }

}