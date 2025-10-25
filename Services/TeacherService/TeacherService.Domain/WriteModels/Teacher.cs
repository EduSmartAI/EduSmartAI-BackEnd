using System;
using System.Collections.Generic;

namespace TeacherService.Domain.WriteModels;

public partial class Teacher
{
    public Guid TeacherId { get; set; }

    public Guid? UserId { get; set; }

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

    public virtual ICollection<TeacherCertificate> TeacherCertificates { get; set; } = new List<TeacherCertificate>();

    public virtual ICollection<TeacherExperience> TeacherExperiences { get; set; } = new List<TeacherExperience>();

    public virtual ICollection<TeacherQualification> TeacherQualifications { get; set; } = new List<TeacherQualification>();

    public virtual ICollection<TeacherRating> TeacherRatings { get; set; } = new List<TeacherRating>();
}
