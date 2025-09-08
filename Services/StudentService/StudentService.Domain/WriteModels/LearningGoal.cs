using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

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

    public virtual ICollection<StudentLearningGoal> StudentLearningGoals { get; set; } = new List<StudentLearningGoal>();
}
