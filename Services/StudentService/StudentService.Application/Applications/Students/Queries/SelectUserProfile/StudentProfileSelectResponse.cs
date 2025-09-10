using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Queries.SelectUserProfile;

public record StudentProfileSelectResponse : AbstractApiResponse<StudentProfileSelectEntity>
{
    public override StudentProfileSelectEntity Response { get; set; }
}

public record StudentProfileSelectEntity
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