using StudentService.Domain.Models;

namespace StudentService.Domain.Model;

public partial class StudentLearningGoal
{
    public Guid StudentId { get; set; }

    public Guid GoalId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual LearningGoal Goal { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
