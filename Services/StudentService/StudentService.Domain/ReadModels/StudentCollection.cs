using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class StudentCollection
{
    public Guid StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public short? Gender { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public short? Marjor { get; set; }
    public short? SkillLevel { get; set; }
    public string? Bio { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool? IsActive { get; set; }
    
    public static StudentCollection FromWriteModel(Student model)
    {
        return new StudentCollection
        {
            StudentId = model.StudentId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            AvatarUrl = model.AvatarUrl,
            Address = model.Address,
            Marjor = model.Marjor,
            SkillLevel = model.SkillLevel,
            Bio = model.Bio,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }
}