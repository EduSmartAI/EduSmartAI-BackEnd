namespace StudentService.Domain.ReadModels;

public class StudentOrientationCollection
{
    public Guid StudentOrientationId { get; set; }

    public Guid StudentId { get; set; }

    public string Technology { get; set; } = null!;

    public string ReasonRecommend { get; set; } = null!;
    
    public short RecommendType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public static StudentOrientationCollection FromWriteModel(WriteModels.StudentOrientation model)
    {
        var result = new StudentOrientationCollection
        {
            StudentOrientationId = model.StudentOrientationId,
            StudentId = model.StudentId,
            Technology = model.Technology,
            RecommendType = model.RecommendType,
            ReasonRecommend = model.ReasonRecommend,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
        };
        return result;
    }
}