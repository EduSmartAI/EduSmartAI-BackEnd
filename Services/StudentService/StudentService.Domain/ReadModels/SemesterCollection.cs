using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class SemesterCollection
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
    
    public static SemesterCollection FromWriteModel(Semester model)
    {
        return new SemesterCollection
        {
            SemesterId = model.SemesterId,
            SemesterName = model.SemesterName,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
        };
    }
}