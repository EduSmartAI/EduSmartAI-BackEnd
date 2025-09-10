namespace StudentService.Domain.ReadModels;

public sealed class LearningGoalCollection
{
    public Guid GoalId { get; set; }

    public string GoalName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public static LearningGoalCollection FromWriteModel(WriteModels.LearningGoal model)
    {
        return new LearningGoalCollection
        {
            GoalId = model.GoalId,
            GoalName = model.GoalName,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
        };
    }
}