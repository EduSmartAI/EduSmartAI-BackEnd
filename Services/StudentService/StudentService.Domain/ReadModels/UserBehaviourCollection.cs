namespace StudentService.Domain.ReadModels;

public class UserBehaviourCollection
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public string ActionType { get; set; } = null!;

    public Guid? TargetId { get; set; }

    public string? TargetType { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual StudentCollection Student { get; set; } = null!;
    
    public static UserBehaviourCollection FromWriteModel(WriteModels.UserBehaviour model, StudentCollection? student = null)
    {
        var userBehaviourCollection = new UserBehaviourCollection
        {
            Id = model.Id,
            StudentId = model.StudentId,
            ActionType = model.ActionType,
            TargetId = model.TargetId,
            TargetType = model.TargetType,
            Metadata = model.Metadata,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
        if (student != null)
        {
            userBehaviourCollection.Student = student;
        }
        else if (model.Student != null)
        {
            userBehaviourCollection.Student = StudentCollection.FromWriteModel(model.Student);
        }

        return userBehaviourCollection;
    }
}