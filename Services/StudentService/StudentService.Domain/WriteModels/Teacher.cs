namespace StudentService.Domain.WriteModels;

public partial class Teacher
{
    public Guid TeacherId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public short? Gender { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Address { get; set; }
    
    public string? PhoneNumber { get; set; }

    public short? Marjor { get; set; }

    public string? Bio { get; set; }

    public short? Degree { get; set; }

    public short? Specialization { get; set; }

    public short? TeachingExperienceYears { get; set; }

    public bool? IsVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<TeacherRating> TeacherRatings { get; set; } = new List<TeacherRating>();
}
