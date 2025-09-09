using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class TechnologyCollection
{
    public Guid TechnologyId { get; set; }
    public string TechnologyName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }
    public ICollection<TypeCollection> Types { get; set; } = new List<TypeCollection>();
    public static TechnologyCollection FromWriteModel(Technology model, bool includeTypes = false)
    {
        var techCollection = new TechnologyCollection
        {
            TechnologyId = model.TechnologyId,
            TechnologyName = model.TechnologyName,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (includeTypes && model.TechnologyTypes.Any())
        {
            techCollection.Types = model.TechnologyTypes
                .Where(tt => true)
                .Select(tt => TypeCollection.FromWriteModel(tt.Type))
                .ToList();
        }

        return techCollection;
    }
}