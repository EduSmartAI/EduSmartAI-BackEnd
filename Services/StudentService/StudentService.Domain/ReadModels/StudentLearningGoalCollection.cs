using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

/// <summary>
/// Read model for StudentLearningGoal with denormalized data for optimized querying
/// </summary>
public class StudentLearningGoalCollection
{
    public string Id => $"{StudentId}_{GoalId}";

    public Guid StudentId { get; set; }

    public Guid GoalId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    /// <summary>
    /// Denormalized learning goal information for optimized reading
    /// </summary>
    public virtual LearningGoalCollection? Goal { get; set; }

    /// <summary>
    /// Creates StudentLearningGoalCollection from write model
    /// </summary>
    /// <param name="model">The write model to convert</param>
    /// <param name="learningGoal">Optional learning goal collection for denormalization</param>
    /// <returns>StudentLearningGoalCollection instance</returns>
    public static StudentLearningGoalCollection FromWriteModel(StudentLearningGoal model, LearningGoalCollection? learningGoal = null)
    {
        var studentGoalCollection = new StudentLearningGoalCollection
        {
            StudentId = model.StudentId,
            GoalId = model.GoalId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        // Add denormalized learning goal data if provided
        if (learningGoal != null)
        {
            studentGoalCollection.Goal = learningGoal;
        }
        else if (model.Goal != null)
        {
            studentGoalCollection.Goal = LearningGoalCollection.FromWriteModel(model.Goal);
        }

        return studentGoalCollection;
    }
}
