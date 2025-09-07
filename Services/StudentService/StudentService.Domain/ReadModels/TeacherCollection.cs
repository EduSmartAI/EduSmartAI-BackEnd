using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class TeacherCollection
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
    public List<TeacherRatingCollection>? TeacherRatings { get; set; } = new();

    public static TeacherCollection FromWriteModel(Teacher model, bool includeRelated = false)
    {
        var result = new TeacherCollection
        {
            TeacherId = model.TeacherId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            AvatarUrl = model.AvatarUrl,
            Address = model.Address,
            Marjor = model.Marjor,
            Bio = model.Bio,
            Degree = model.Degree,
            Specialization = model.Specialization,
            TeachingExperienceYears = model.TeachingExperienceYears,
            IsVerified = model.IsVerified,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            PhoneNumber = model.PhoneNumber
        };

        if (includeRelated)
        {
            result.TeacherRatings = TeacherRatingCollection.FromWriteModel(model.TeacherRatings);
        }

        return result;
    }
}