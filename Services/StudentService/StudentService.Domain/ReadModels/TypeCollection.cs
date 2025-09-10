namespace StudentService.Domain.ReadModels;

public sealed class TypeCollection
{
    public Guid TypeId { get; set; }
    public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }

    public static TypeCollection FromWriteModel(WriteModels.Type model)
    {
        return new TypeCollection
        {
            TypeId = model.TypeId,
            TypeName = model.TypeName,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }
}