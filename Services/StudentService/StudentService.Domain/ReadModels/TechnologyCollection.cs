using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class TechnologyCollection
{
    public Guid TechnologyId { get; set; }
    public string TechnologyName { get; set; } = null!;
    public string? Description { get; set; }
    public short TechnologyType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }
    public static TechnologyCollection FromWriteModel(Technology model)
    {
        var techCollection = new TechnologyCollection
        {
            TechnologyId = model.TechnologyId,
            TechnologyName = model.TechnologyName,
            Description = model.Description,
            TechnologyType = model.TechnologyType,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        return techCollection;
    }
}