using TeacherService.Domain.WriteModels;

namespace TeacherService.Domain.ReadModels;

public class TeacherCollection
{
    public Guid TeacherId { get; set; }
    
    public string DisplayName { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Bio { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual List<TeacherCertificateCollection>? TeacherCertificates { get; set; }

    public virtual List<TeacherExperienceCollection>? TeacherExperiences { get; set; }

    public virtual List<TeacherQualificationCollection>? TeacherQualifications { get; set; }

    public virtual List<TeacherRatingCollection>? TeacherRatings { get; set; }
    
    public static TeacherCollection FromWriteModel(Teacher teacher)
    {        
        var collection = new TeacherCollection
        {
            TeacherId = teacher.TeacherId,
            DisplayName = teacher.DisplayName,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Bio = teacher.Bio,
            ProfilePictureUrl = teacher.ProfilePictureUrl,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt,
            CreatedBy = teacher.CreatedBy,
            UpdatedBy = teacher.UpdatedBy,
            IsActive = teacher.IsActive,
            TeacherCertificates = teacher.TeacherCertificates?.Select(TeacherCertificateCollection.FromWriteModel).ToList(),
            TeacherExperiences = teacher.TeacherExperiences?.Select(TeacherExperienceCollection.FromWriteModel).ToList(),
            TeacherQualifications = teacher.TeacherQualifications?.Select(TeacherQualificationCollection.FromWriteModel).ToList(),
            TeacherRatings = teacher.TeacherRatings?.Select(TeacherRatingCollection.FromWriteModel).ToList()
        };
        return collection;
        
    }

}