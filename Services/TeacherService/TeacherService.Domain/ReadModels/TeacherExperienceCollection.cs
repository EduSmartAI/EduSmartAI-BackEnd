using TeacherService.Domain.WriteModels;

namespace TeacherService.Domain.ReadModels;

public class TeacherExperienceCollection
{
    public Guid ExperienceId { get; set; }

    public Guid TeacherId { get; set; }

    public string? RoleTitle { get; set; }

    public string? Organization { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public static TeacherExperienceCollection FromWriteModel(TeacherExperience experience)
    {
        var collection = new TeacherExperienceCollection
        {
            ExperienceId = experience.ExperienceId,
            TeacherId = experience.TeacherId,
            RoleTitle = experience.RoleTitle,
            Organization = experience.Organization,
            StartDate = experience.StartDate,
            EndDate = experience.EndDate,
            IsCurrent = experience.IsCurrent,
            Description = experience.Description,
            CreatedAt = experience.CreatedAt,
            UpdatedAt = experience.UpdatedAt,
            CreatedBy = experience.CreatedBy,
            UpdatedBy = experience.UpdatedBy,
            IsActive = experience.IsActive
        };
        return collection;
    }
}