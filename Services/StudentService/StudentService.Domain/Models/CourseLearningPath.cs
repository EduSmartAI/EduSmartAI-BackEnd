using StudentService.Domain.Model;

namespace StudentService.Domain.Models;

public partial class CourseLearningPath
{
    public Guid CourseId { get; set; }

    public Guid PathId { get; set; }

    public int? Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual LearningPath Path { get; set; } = null!;
}
