namespace StudentService.Domain.ReadModels;

public class LearningPathCollection
{
    public Guid PathId { get; set; }
    public string PathName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }
    public Guid? StudentId { get; set; }
    public short Status { get; set; }

    public List<LearningPathMajorCollection> LearningPathMajors { get; set; }

    public static LearningPathCollection FromWriteModel(WriteModels.LearningPath model)
    {
        return new LearningPathCollection
        {
            PathId = model.PathId,
            PathName = model.PathName,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            StudentId = model.StudentId,
            Status = model.Status,
            LearningPathMajors = (model.LearningPathMajors ?? new List<WriteModels.LearningPathMajor>())
                .Select(LearningPathMajorCollection.FromWriteModel)
                .ToList()
        };
    }

}