namespace StudentService.Domain.ReadModels;

public class LearningPathCollection
{
    public Guid PathId { get; set; }

    public string PathName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid? StudentId { get; set; }

    public virtual ICollection<LearningPathMajorCollection> LearningPathMajors { get; set; } = new List<LearningPathMajorCollection>();
    public static LearningPathCollection FromWriteModel(WriteModels.LearningPath model)
    {
        return new LearningPathCollection
        {
            PathId = model.PathId,
            PathName = model.PathName,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            StudentId = model.StudentId,
            LearningPathMajors = model.LearningPathMajors
                .Select(LearningPathMajorCollection.FromWriteModel)
                .ToList()
        };
    }

}