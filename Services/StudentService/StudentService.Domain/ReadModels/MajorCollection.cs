using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class MajorCollection
{
    public Guid MajorId { get; set; }

    public string MajorName { get; set; } = null!;
    
    public string MajorCode { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public static MajorCollection FromWriteModel(Major model)
    {
        return new MajorCollection
        {
            MajorId = model.MajorId,
            MajorName = model.MajorName,
            MajorCode = model.MajorCode,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
        };
    }
}