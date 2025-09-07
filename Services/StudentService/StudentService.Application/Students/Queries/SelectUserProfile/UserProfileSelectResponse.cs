using BaseService.Common.ApiEntities;

namespace StudentService.Application.Students.Queries.SelectUserProfile;

public record UserProfileSelectResponse : AbstractApiResponse<UserProfileSelectEntity>
{
    public override UserProfileSelectEntity Response { get; set; }
}

public record UserProfileSelectEntity
{
    public LecturerProfile? TeacherProfile { get; set; }
    public StudentProfile? StudentProfile { get; set; }
}

public class LecturerProfile
{
    public Guid LecturerId { get; set; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AvatarUrl { get; init; }
    public DateOnly? DateOfBirth { get; set; }
    public short? Gender { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public short? Marjor { get; set; }
    public short? Degree { get; set; }
    public short? Specialization { get; set; }
    public short? TeachingExperienceYears { get; set; }
    public bool? IsVerified { get; set; }
}
public class StudentProfile
{
    public Guid StudentId { get; set; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AvatarUrl { get; init; }
    public DateOnly? DateOfBirth { get; set; }
    public short? Gender { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public short? Marjor { get; set; }
    public short? SkillLevel { get; set; }
}