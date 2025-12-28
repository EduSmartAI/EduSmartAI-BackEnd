using TeacherService.Domain.WriteModels;

namespace TeacherService.Domain.ReadModels;

public class TeacherQualificationCollection
{
    public Guid QualificationId { get; set; }

    public Guid TeacherId { get; set; }

    public string? DegreeTitle { get; set; }

    public string? Institution { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }

    public string? CertificateUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }
    
    public static TeacherQualificationCollection FromWriteModel(TeacherQualification qualification)
    {
        var collection = new TeacherQualificationCollection
        {
            QualificationId = qualification.QualificationId,
            TeacherId = qualification.TeacherId,
            DegreeTitle = qualification.DegreeTitle,
            Institution = qualification.Institution,
            StartDate = qualification.StartDate,
            EndDate = qualification.EndDate,
            Description = qualification.Description,
            CertificateUrl = qualification.CertificateUrl,
            CreatedAt = qualification.CreatedAt,
            UpdatedAt = qualification.UpdatedAt,
            CreatedBy = qualification.CreatedBy,
            UpdatedBy = qualification.UpdatedBy,
            IsActive = qualification.IsActive
        };
        return collection;
    }
}