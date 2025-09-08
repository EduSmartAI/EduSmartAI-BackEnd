namespace StudentService.Domain.Models;

public partial class LearningGoal
{
    public Guid GoalId { get; set; }

    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
}
