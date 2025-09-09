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

    public ICollection<CourseLearningPathCollection> CourseLearningPaths { get; set; } = new List<CourseLearningPathCollection>();
    
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
            CourseLearningPaths = model.CourseLearningPaths
                .Select(CourseLearningPathCollection.FromWriteModel)
                .ToList()
        };
    }

}