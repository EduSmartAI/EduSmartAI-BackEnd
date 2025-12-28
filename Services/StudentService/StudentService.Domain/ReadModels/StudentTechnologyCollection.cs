using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class StudentTechnologyCollection
{
    public string Id => $"{StudentId}_{TechnologyId}";

    public Guid StudentId { get; set; }

    public Guid TechnologyId { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; }
    
    public virtual TechnologyCollection Technology { get; set; } = null!;
    
    public static StudentTechnologyCollection FromWriteModel(StudentTechnology model, TechnologyCollection? technology = null)
    {
        var studentTechCollection = new StudentTechnologyCollection
        {
            StudentId = model.StudentId,
            TechnologyId = model.TechnologyId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
        if (technology != null)
        {
            studentTechCollection.Technology = technology;
        }
        else if (model.Technology != null)
        {
            studentTechCollection.Technology = TechnologyCollection.FromWriteModel(model.Technology);
        }

        return studentTechCollection;
    }
}
